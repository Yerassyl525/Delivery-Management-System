using DeliveryFinalProject;
using Xunit;

namespace DeliveryFinalProject.Tests;

public class Assignment8Tests
{
    private static DeliveryQuote[] SampleQuotes() => new[]
    {
        new DeliveryQuote("A", 2m, "standard", 700m),
        new DeliveryQuote("B", 5m, "express", 1550m),
        new DeliveryQuote("C", 0m, "standard", 500m),
        new DeliveryQuote("D", 5m, "express", 1550m)
    };

    [Fact]
    public void ForCategoryExtensionFiltersWithoutChangingSource()
    {
        var quotes = SampleQuotes();
        var result = quotes.ForCategory("EXPRESS").ToArray();
        Assert.Equal(new[] { "B", "D" }, result.Select(item => item.OrderId));
        Assert.Equal(4, quotes.Length);
    }

    [Fact]
    public void TotalCostExtensionAddsAllQuoteCosts()
    {
        Assert.Equal(4300m, SampleQuotes().TotalCost());
        Assert.Equal(0m, Array.Empty<DeliveryQuote>().TotalCost());
    }

    [Fact]
    public void ExpressReportFiltersProjectsAndSortsTiesByOrderId()
    {
        var report = DeliveryReports.GetExpressOrders(SampleQuotes());
        Assert.Equal(new[] { "B", "D" }, report.Select(row => row.OrderId));
        Assert.All(report, row => Assert.Equal("express", row.Category));
    }

    [Fact]
    public void ExpressReportReturnsEmptyWhenThereAreNoMatches()
    {
        Assert.Empty(DeliveryReports.GetExpressOrders(new[]
        {
            new DeliveryQuote("A", 1m, "standard", 600m)
        }));
    }

    [Fact]
    public void ReportsHandleEmptyDataset()
    {
        Assert.Empty(DeliveryReports.GetExpressOrders(Array.Empty<DeliveryQuote>()));
        Assert.Empty(DeliveryReports.GetCategorySummary(Array.Empty<DeliveryQuote>()));
        Assert.Equal(0m, DeliveryReports.CalculateTotal(Array.Empty<DeliveryQuote>()));
    }

    [Fact]
    public void CategoryReportGroupsAndCalculatesCountSumAndAverage()
    {
        var report = DeliveryReports.GetCategorySummary(SampleQuotes());
        var express = Assert.Single(report.Where(row => row.Category == "express"));
        Assert.Equal(2, express.OrderCount);
        Assert.Equal(3100m, express.TotalCost);
        Assert.Equal(1550m, express.AverageCost);
    }

    [Fact]
    public void CategoryReportUsesDeterministicTieBreaker()
    {
        var quotes = new[]
        {
            new DeliveryQuote("Z", 1m, "zeta", 100m),
            new DeliveryQuote("A", 1m, "alpha", 100m)
        };
        var report = DeliveryReports.GetCategorySummary(quotes);
        Assert.Equal(new[] { "alpha", "zeta" }, report.Select(row => row.Category));
    }

    [Fact]
    public void CurriedConfigurationsReturnDifferentScoresForSameQuote()
    {
        var quote = new DeliveryQuote("A", 2m, "standard", 700m);
        var standard = DeliveryScoring.StandardConfiguration(quote);
        var priority = DeliveryScoring.PriorityConfiguration(quote);
        Assert.Equal(20m, standard);
        Assert.Equal(40m, priority);
        Assert.NotEqual(standard, priority);
    }

    [Fact]
    public void CurriedExpressScoreIncludesConfiguredBonus()
    {
        var quote = new DeliveryQuote("B", 5m, "express", 1550m);
        var score = DeliveryScoring.CreateScore(10m)(50m);
        Assert.Equal(100m, score(quote));
    }

    [Fact]
    public void ScoringWorkflowAcceptsFunctionAsArgumentAndSortsDeterministically()
    {
        var report = DeliveryScoring.BuildReport(SampleQuotes(), DeliveryScoring.StandardConfiguration);
        Assert.Equal(new[] { "B", "D", "A", "C" }, report.Select(row => row.OrderId));
    }

    [Fact]
    public void DeferredWorkflowDoesNotRunUntilEnumeration()
    {
        var calls = 0;
        IEnumerable<DeliveryOrderRequest> Source()
        {
            calls++;
            yield return new DeliveryOrderRequest("A", "2", "Standard");
        }

        var query = DeferredProcessing.StreamValidQuotes(Source());
        Assert.Equal(0, calls);
        using var iterator = query.GetEnumerator();
        Assert.Equal(0, calls);
        Assert.True(iterator.MoveNext());
        Assert.Equal(1, calls);
        Assert.Equal("A", iterator.Current.OrderId);
    }

    [Fact]
    public void MaterializedSnapshotCanBeReusedWithoutReprocessingSource()
    {
        var calls = 0;
        IEnumerable<DeliveryOrderRequest> Source()
        {
            calls++;
            yield return new DeliveryOrderRequest("A", "2", "Standard");
        }

        var snapshot = DeferredProcessing.CreateSnapshot(Source());
        Assert.Equal(1, calls);
        Assert.Single(snapshot);
        Assert.Single(snapshot);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void DeferredWorkflowSkipsInvalidRequestsAndKeepsValidOnes()
    {
        var result = DeferredProcessing.StreamValidQuotes(new[]
        {
            new DeliveryOrderRequest("A", "2", "Standard"),
            new DeliveryOrderRequest("B", "-1", "Standard")
        }).ToArray();
        Assert.Single(result);
        Assert.Equal("A", result[0].OrderId);
    }

    [Fact]
    public void NativeAdapterRejectsInvalidDomainInputBeforeCallingLibrary()
    {
        var adapter = new NativeScoreCalculator();
        var result = adapter.Calculate(new DeliveryQuote("A", -1m, "standard", 0m));
        Assert.True(result.IsFailure);
        Assert.Contains("between 0 and 1000", result.Error);
    }

    [Fact]
    public void NativeAdapterRealCallReturnsManuallyCalculatedValue()
    {
        var adapter = new NativeScoreCalculator();
        var result = adapter.Calculate(new DeliveryQuote("B", 5m, "express", 1550m));
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public void WorkflowUsesNativeAdapterAndReturnsScores()
    {
        var summary = DeliveryPipeline.Run(new[]
        {
            new DeliveryOrderRequest("A", "2", "Standard"),
            new DeliveryOrderRequest("B", "5", "Express")
        });
        var result = DeliveryA8Application.BuildResult(summary, new NativeScoreCalculator());
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(new[] { 20, 100 }, result.Value.NativeScores.Select(row => row.Score));
        Assert.Single(result.Value.ExpressOrders);
        Assert.Equal(2, result.Value.CategorySummary.Count);
    }

    [Fact]
    public void WorkflowConvertsNativeAdapterFailureToResultFailure()
    {
        var summary = DeliveryPipeline.Run(new[]
        {
            new DeliveryOrderRequest("A", "2", "Standard")
        });
        var result = DeliveryA8Application.BuildResult(summary, new FailingNativeCalculator());
        Assert.True(result.IsFailure);
        Assert.Contains("Native score failed for order A", result.Error);
    }

    [Fact]
    public void ExportCreatesCsvReportAndWritesHeader()
    {
        var directory = Path.Combine(Path.GetTempPath(), "delivery-a8-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "category.csv");
        try
        {
            var report = DeliveryReports.GetCategorySummary(SampleQuotes());
            var result = DeliveryA8Application.ExportCategoryReport(report, path);
            Assert.True(result.IsSuccess, result.Error);
            Assert.Contains("Category,OrderCount,TotalCost,AverageCost", File.ReadAllText(path));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private sealed class FailingNativeCalculator : INativeScoreCalculator
    {
        public Result<int> Calculate(DeliveryQuote quote) => Result<int>.Failure("Simulated native binding failure.");
    }
}
