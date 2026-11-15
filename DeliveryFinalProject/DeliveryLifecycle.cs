namespace DeliveryFinalProject;

public sealed class PricedDeliveryOrder
{
    private PricedDeliveryOrder(ValidatedDeliveryOrder order, DeliveryQuote quote)
    {
        Order = order;
        Quote = quote;
    }

    public ValidatedDeliveryOrder Order { get; }
    public DeliveryQuote Quote { get; }

    public static Result<PricedDeliveryOrder> Create(
        ValidatedDeliveryOrder order,
        Func<ValidatedDeliveryOrder, decimal>? pricingRule = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        var quote = DeliveryPipeline.CalculateQuote(
            new DeliveryOrderValidation.Valid(order),
            pricingRule ?? (item => item.Category switch
            {
                "standard" => PricingPolicies.Standard(item.DistanceKm),
                "express" => PricingPolicies.Express(item.DistanceKm),
                _ => -1m
            }));

        return quote.Map(value => new PricedDeliveryOrder(order, value));
    }
}

public sealed class ApprovedDeliveryOrder
{
    private ApprovedDeliveryOrder(PricedDeliveryOrder pricedOrder) => PricedOrder = pricedOrder;

    public PricedDeliveryOrder PricedOrder { get; }

    public static Result<ApprovedDeliveryOrder> Approve(
        PricedDeliveryOrder pricedOrder, bool paymentConfirmed)
    {
        ArgumentNullException.ThrowIfNull(pricedOrder);
        return paymentConfirmed
            ? Result<ApprovedDeliveryOrder>.Success(new ApprovedDeliveryOrder(pricedOrder))
            : Result<ApprovedDeliveryOrder>.Failure("Delivery cannot be approved before payment is confirmed.");
    }
}

public sealed class DispatchedDeliveryOrder
{
    private DispatchedDeliveryOrder(ApprovedDeliveryOrder approvedOrder, string trackingCode)
    {
        ApprovedOrder = approvedOrder;
        TrackingCode = trackingCode;
    }

    public ApprovedDeliveryOrder ApprovedOrder { get; }
    public string TrackingCode { get; }

    public static Result<DispatchedDeliveryOrder> Dispatch(
        ApprovedDeliveryOrder approvedOrder, string trackingCode)
    {
        ArgumentNullException.ThrowIfNull(approvedOrder);
        return string.IsNullOrWhiteSpace(trackingCode)
            ? Result<DispatchedDeliveryOrder>.Failure("A tracking code is required before dispatch.")
            : Result<DispatchedDeliveryOrder>.Success(
                new DispatchedDeliveryOrder(approvedOrder, trackingCode.Trim()));
    }
}

public static class DeliveryOrderWorkflow
{
    public static Result<DispatchedDeliveryOrder> ApproveAndDispatch(
        DeliveryOrderRequest request,
        bool paymentConfirmed,
        string trackingCode,
        Func<ValidatedDeliveryOrder, decimal>? pricingRule = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = ValidatedDeliveryOrder.Create(request.Id, request.DistanceText, request.Category);
        return validation switch
        {
            DeliveryOrderValidation.Valid valid => PricedDeliveryOrder.Create(valid.Order, pricingRule)
                .Bind(priced => ApprovedDeliveryOrder.Approve(priced, paymentConfirmed))
                .Bind(approved => DispatchedDeliveryOrder.Dispatch(approved, trackingCode)),
            DeliveryOrderValidation.Invalid invalid =>
                Result<DispatchedDeliveryOrder>.Failure(string.Join(" ", invalid.Errors)),
            _ => Result<DispatchedDeliveryOrder>.Failure("Unknown validation state.")
        };
    }
}
