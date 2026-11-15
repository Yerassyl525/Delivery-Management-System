using System.Text.Json;
using DeliveryFinalProject;
using Xunit;

namespace DeliveryFinalProject.Tests;

public class Assignment7Tests
{
    [Fact]
    public void LoadRequests_ReadsValidJsonFile()
    {
        var path = WriteTempFile("[{\"id\":\"A\",\"distanceText\":\"2\",\"category\":\"Standard\"}]");
        try
        {
            var result = DeliveryFileService.LoadRequests(path);
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Equal("A", result.Value[0].Id);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadRequests_MissingFileReturnsFailure()
    {
        var result = DeliveryFileService.LoadRequests(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadRequests_MalformedJsonReturnsFailureInsteadOfThrowing()
    {
        var path = WriteTempFile("[{ invalid json }");
        try
        {
            var result = DeliveryFileService.LoadRequests(path);
            Assert.True(result.IsFailure);
            Assert.Contains("malformed", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadRequests_JsonNullReturnsFailure()
    {
        var path = WriteTempFile("null");
        try
        {
            var result = DeliveryFileService.LoadRequests(path);
            Assert.True(result.IsFailure);
            Assert.Contains("array", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ProcessFile_ComposesBindAndMapForRealWorkflow()
    {
        var path = WriteTempFile("[{\"id\":\"A\",\"distanceText\":\"2\",\"category\":\"Standard\"}]");
        try
        {
            var result = DeliveryApplication.ProcessFile(path);
            Assert.True(result.IsSuccess);
            Assert.Equal(700m, result.Value.TotalCost);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ProcessFile_EmptyArrayFailsAndSkipsMapping()
    {
        var path = WriteTempFile("[]");
        var mapCalls = 0;
        try
        {
            var load = DeliveryFileService.LoadRequests(path);
            var result = load.Bind(requests => requests.Count == 0
                ? Result<IReadOnlyList<DeliveryOrderRequest>>.Failure("empty")
                : Result<IReadOnlyList<DeliveryOrderRequest>>.Success(requests))
                .Map(requests => { mapCalls++; return DeliveryPipeline.Run(requests); });
            Assert.True(result.IsFailure);
            Assert.Equal(0, mapCalls);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MapAndBindPropagateFailureWithoutCallingDelegates()
    {
        var calls = 0;
        var result = Result<int>.Failure("source failed")
            .Map(value => { calls++; return value + 1; })
            .Bind(value => { calls++; return Result<string>.Success(value.ToString()); });
        Assert.True(result.IsFailure);
        Assert.Equal("source failed", result.Error);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void BindCanConvertSuccessfulResultToDomainFailure()
    {
        var result = Result<int>.Success(0).Bind(value => value > 0
            ? Result<string>.Success(value.ToString())
            : Result<string>.Failure("Value must be positive."));
        Assert.True(result.IsFailure);
        Assert.Equal("Value must be positive.", result.Error);
    }

    [Fact]
    public void OptionSomeRepresentsFoundQuote()
    {
        var summary = DeliveryPipeline.Run(new[] { new DeliveryOrderRequest("A", "2", "Standard") });
        var option = DeliveryApplication.FindQuote(summary, "A");
        var some = Assert.IsType<Option<DeliveryQuote>.Some>(option);
        Assert.Equal(700m, some.Value.Cost);
    }

    [Fact]
    public void OptionNoneRepresentsMissingQuoteWithoutException()
    {
        var summary = DeliveryPipeline.Run(Array.Empty<DeliveryOrderRequest>());
        var option = DeliveryApplication.FindQuote(summary, "MISSING");
        Assert.IsType<Option<DeliveryQuote>.None>(option);
        Assert.Equal("missing", option.Match(_ => "found", () => "missing"));
    }

    [Fact]
    public void DomainInvalidOrderIsRejectedAndOtherOrdersStillProcess()
    {
        var summary = DeliveryPipeline.Run(new[]
        {
            new DeliveryOrderRequest("A", "2", "Standard"),
            new DeliveryOrderRequest("B", "bad-number", "Standard")
        });
        Assert.Single(summary.Quotes);
        Assert.Equal(700m, summary.TotalCost);
        Assert.Equal(1, summary.RejectedOrders);
    }

    [Fact]
    public void ProcessFile_MalformedJsonStopsBeforeDomainPipeline()
    {
        var path = WriteTempFile("not-json");
        try
        {
            var result = DeliveryApplication.ProcessFile(path);
            Assert.True(result.IsFailure);
            Assert.Contains("malformed", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally { File.Delete(path); }
    }

    private static string WriteTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"delivery-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }
}
