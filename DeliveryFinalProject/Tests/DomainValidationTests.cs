using DeliveryFinalProject;
using Xunit;

namespace DeliveryFinalProject.Tests;

public class DeliveryPipelineTests
{
    [Fact]
    public void FactorySuccess_CreatesValidatedOrderWithExpectedData()
    {
        var result = ValidatedDeliveryOrder.Create(" A ", "2", "Standard");
        var valid = Assert.IsType<DeliveryOrderValidation.Valid>(result);
        Assert.Equal("A", valid.Order.Id);
        Assert.Equal(2m, valid.Order.DistanceKm);
        Assert.Equal("standard", valid.Order.Category);
    }

    [Fact]
    public void FactoryRejectsMissingId()
    {
        var result = Assert.IsType<DeliveryOrderValidation.Invalid>(
            ValidatedDeliveryOrder.Create("", "2", "Standard"));
        Assert.Contains(result.Errors, error => error.Contains("ID"));
    }

    [Fact]
    public void FactoryRejectsNegativeDistance()
    {
        var result = Assert.IsType<DeliveryOrderValidation.Invalid>(
            ValidatedDeliveryOrder.Create("A", "-0.1", "Standard"));
        Assert.Contains(result.Errors, error => error.Contains("negative"));
    }

    [Fact]
    public void FactoryRejectsUnknownCategory()
    {
        var result = Assert.IsType<DeliveryOrderValidation.Invalid>(
            ValidatedDeliveryOrder.Create("A", "2", "Overnight"));
        Assert.Contains(result.Errors, error => error.Contains("Category"));
    }

    [Fact]
    public void FactoryRejectsNonNumericDistance()
    {
        var result = Assert.IsType<DeliveryOrderValidation.Invalid>(
            ValidatedDeliveryOrder.Create("A", "abc", "Standard"));
        Assert.Contains(result.Errors, error => error.Contains("valid number"));
    }

    [Fact]
    public void PatternMatchingHandlesValidCaseAndNormalWorkflowCalculatesQuote()
    {
        var validation = ValidatedDeliveryOrder.Create("A", "2", "Standard");
        var result = DeliveryPipeline.CalculateQuote(validation);
        Assert.True(result.IsSuccess);
        Assert.Equal(700m, result.Value.Cost);
    }

    [Fact]
    public void PatternMatchingHandlesInvalidCaseWithoutQuote()
    {
        var validation = ValidatedDeliveryOrder.Create("A", "-2", "Standard");
        var result = DeliveryPipeline.CalculateQuote(validation);
        Assert.True(result.IsFailure);
        Assert.Contains("negative", result.Error);
    }

    [Fact]
    public void MissingPrerequisite_IsRejected()
    {
        var invalid = new DeliveryOrderValidation.Invalid(new[] { "Validation is required." });
        var result = DeliveryPipeline.CalculateQuote(invalid);
        Assert.True(result.IsFailure);
        Assert.Equal("Validation is required.", result.Error);
    }

    [Fact]
    public void RepeatedCalculationReturnsSameOutcome()
    {
        var validation = ValidatedDeliveryOrder.Create("A", "2", "Express");
        var first = DeliveryPipeline.CalculateQuote(validation);
        var second = DeliveryPipeline.CalculateQuote(validation);
        Assert.Equal(first.Value, second.Value);
        Assert.Equal(1100m, second.Value.Cost);
    }

    [Fact]
    public void ZeroDistanceBoundaryIsAccepted()
    {
        var validation = ValidatedDeliveryOrder.Create("A", "0", "Standard");
        var result = DeliveryPipeline.CalculateQuote(validation);
        Assert.True(result.IsSuccess);
        Assert.Equal(500m, result.Value.Cost);
    }

    [Fact]
    public void FailedFactoryAndTransitionDoNotChangeOriginalRequest()
    {
        var request = new DeliveryOrderRequest("A", "-3", "Standard");
        var before = request with { };
        var validation = ValidatedDeliveryOrder.Create(request.Id, request.DistanceText, request.Category);
        var result = DeliveryPipeline.CalculateQuote(validation);
        Assert.True(result.IsFailure);
        Assert.Equal(before, request);
    }

    [Fact]
    public void PipelineSummarizesValidAndRejectedRequests()
    {
        var requests = new[]
        {
            new DeliveryOrderRequest("A", "2", "Standard"),
            new DeliveryOrderRequest("B", "5", "Express"),
            new DeliveryOrderRequest("C", "-1", "Standard")
        };
        var summary = DeliveryPipeline.Run(requests);
        Assert.Equal(2, summary.Quotes.Count);
        Assert.Equal(2250m, summary.TotalCost);
        Assert.Equal(1, summary.RejectedOrders);
    }

    [Fact]
    public void MapAndBindSkipOperationsAfterFailure()
    {
        var calls = 0;
        var result = Result<int>.Failure("Invalid order")
            .Map(value => { calls++; return value * 2; })
            .Bind(value => { calls++; return Result<string>.Success(value.ToString()); });
        Assert.True(result.IsFailure);
        Assert.Equal("Invalid order", result.Error);
        Assert.Equal(0, calls);
    }
}
