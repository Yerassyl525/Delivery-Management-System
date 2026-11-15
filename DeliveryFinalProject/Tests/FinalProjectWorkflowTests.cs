using DeliveryFinalProject;
using Xunit;

namespace DeliveryFinalProject.Tests;

public class FinalProjectWorkflowTests
{
    [Fact]
    public void PipelineRunsThreeNamedStagesAndBuildsExpectedSummary()
    {
        var requests = new[]
        {
            new DeliveryOrderRequest("A", "2", "Standard"),
            new DeliveryOrderRequest("B", "5", "Express"),
            new DeliveryOrderRequest("C", "-1", "Standard")
        };

        var validation = DeliveryPipeline.ValidateOrders(requests);
        var quotes = DeliveryPipeline.CalculateQuotes(validation);
        Assert.True(quotes.IsSuccess);
        var summary = DeliveryPipeline.BuildSummary(quotes.Value);

        Assert.Equal(2, summary.Quotes.Count);
        Assert.Equal(2250m, summary.TotalCost);
        Assert.Equal(1, summary.RejectedOrders);
    }

    [Fact]
    public void HigherOrderPricingFunctionChangesTheBusinessPrice()
    {
        var requests = new[] { new DeliveryOrderRequest("A", "2", "Standard") };
        var result = DeliveryPipeline.Run(requests, order => 1000m + order.DistanceKm * 25m);

        Assert.True(result.IsSuccess);
        Assert.Equal(1050m, result.Value.TotalCost);
    }

    [Fact]
    public void PricingFunctionCannotReturnNegativeCost()
    {
        var validation = ValidatedDeliveryOrder.Create("A", "2", "Standard");
        var result = DeliveryPipeline.CalculateQuote(validation, _ => -1m);
        Assert.True(result.IsFailure);
        Assert.Contains("negative cost", result.Error);
    }

    [Fact]
    public void LifecycleRejectsApprovalBeforePayment()
    {
        var result = DeliveryOrderWorkflow.ApproveAndDispatch(
            new DeliveryOrderRequest("A", "2", "Standard"), false, "TRK-1");

        Assert.True(result.IsFailure);
        Assert.Contains("payment", result.Error);
    }

    [Fact]
    public void LifecycleRejectsDispatchWithoutTrackingCode()
    {
        var result = DeliveryOrderWorkflow.ApproveAndDispatch(
            new DeliveryOrderRequest("A", "2", "Standard"), true, " ");

        Assert.True(result.IsFailure);
        Assert.Contains("tracking", result.Error);
    }

    [Fact]
    public void LifecycleAllowsOnlyValidatedPricedApprovedOrderToBeDispatched()
    {
        var result = DeliveryOrderWorkflow.ApproveAndDispatch(
            new DeliveryOrderRequest("A", "2", "Standard"), true, "TRK-100");

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("TRK-100", result.Value.TrackingCode);
        Assert.Equal(700m, result.Value.ApprovedOrder.PricedOrder.Quote.Cost);
    }

    [Fact]
    public void SaveRequestsWritesJsonThatCanBeLoadedAgain()
    {
        var path = Path.Combine(Path.GetTempPath(), $"delivery-save-{Guid.NewGuid():N}", "orders.json");
        var requests = new[] { new DeliveryOrderRequest("A", "2", "Standard") };
        try
        {
            var saved = DeliveryFileService.SaveRequests(path, requests);
            Assert.True(saved.IsSuccess, saved.Error);
            var loaded = DeliveryFileService.LoadRequests(path);
            Assert.True(loaded.IsSuccess, loaded.Error);
            Assert.Equal(requests[0], loaded.Value[0]);
        }
        finally
        {
            var directory = Path.GetDirectoryName(path)!;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
