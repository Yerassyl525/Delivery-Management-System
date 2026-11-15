namespace DeliveryFinalProject;

public sealed record ValidationBatch(
    IReadOnlyList<ValidatedDeliveryOrder> ValidOrders,
    int RejectedOrders);

public sealed record QuoteBatch(
    IReadOnlyList<DeliveryQuote> Quotes,
    int RejectedOrders);

public static class DeliveryPipeline
{
    public static Result<DeliveryQuote> CalculateQuote(DeliveryOrderValidation validation) =>
        CalculateQuote(validation, DefaultPricingRule);

    public static Result<DeliveryQuote> CalculateQuote(
        DeliveryOrderValidation validation,
        Func<ValidatedDeliveryOrder, decimal> pricingRule)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(pricingRule);

        return validation switch
        {
            DeliveryOrderValidation.Valid valid => CreateQuote(valid.Order, pricingRule),
            DeliveryOrderValidation.Invalid invalid => Result<DeliveryQuote>.Failure(string.Join(" ", invalid.Errors)),
            _ => Result<DeliveryQuote>.Failure("Unknown delivery validation state.")
        };
    }

    public static ValidationBatch ValidateOrders(IReadOnlyList<DeliveryOrderRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var valid = new List<ValidatedDeliveryOrder>();
        var rejected = 0;

        foreach (var request in requests)
        {
            if (request is null)
            {
                rejected++;
                continue;
            }

            switch (ValidatedDeliveryOrder.Create(request.Id, request.DistanceText, request.Category))
            {
                case DeliveryOrderValidation.Valid item:
                    valid.Add(item.Order);
                    break;
                case DeliveryOrderValidation.Invalid:
                    rejected++;
                    break;
            }
        }

        return new ValidationBatch(Array.AsReadOnly(valid.ToArray()), rejected);
    }

    public static Result<QuoteBatch> CalculateQuotes(
        ValidationBatch validation,
        Func<ValidatedDeliveryOrder, decimal>? pricingRule = null)
    {
        ArgumentNullException.ThrowIfNull(validation);
        pricingRule ??= DefaultPricingRule;

        var quotes = new List<DeliveryQuote>();
        foreach (var order in validation.ValidOrders)
        {
            var quote = CreateQuote(order, pricingRule);
            if (quote.IsFailure)
                return Result<QuoteBatch>.Failure(quote.Error!);
            quotes.Add(quote.Value);
        }

        var ordered = quotes
            .OrderBy(item => item.Cost)
            .ThenBy(item => item.OrderId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Result<QuoteBatch>.Success(new QuoteBatch(
            Array.AsReadOnly(ordered), validation.RejectedOrders));
    }

    public static DeliverySummary BuildSummary(QuoteBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        return new DeliverySummary(
            batch.Quotes,
            batch.Quotes.Sum(item => item.Cost),
            batch.RejectedOrders);
    }

    public static DeliverySummary Run(IReadOnlyList<DeliveryOrderRequest> requests)
    {
        var validationStage = ValidateOrders(requests);
        var quoteStage = CalculateQuotes(validationStage);
        if (quoteStage.IsFailure)
            throw new InvalidOperationException(quoteStage.Error);
        return BuildSummary(quoteStage.Value);
    }

    public static Result<DeliverySummary> Run(
        IReadOnlyList<DeliveryOrderRequest> requests,
        Func<ValidatedDeliveryOrder, decimal> pricingRule)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(pricingRule);

        var validationStage = ValidateOrders(requests);
        return CalculateQuotes(validationStage, pricingRule)
            .Map(BuildSummary);
    }

    private static Result<DeliveryQuote> CreateQuote(
        ValidatedDeliveryOrder order,
        Func<ValidatedDeliveryOrder, decimal> pricingRule)
    {
        var cost = pricingRule(order);
        return cost < 0m
            ? Result<DeliveryQuote>.Failure($"Pricing rule returned a negative cost for order {order.Id}.")
            : Result<DeliveryQuote>.Success(new DeliveryQuote(
                order.Id, order.DistanceKm, order.Category, cost));
    }

    private static decimal DefaultPricingRule(ValidatedDeliveryOrder order) => order.Category switch
    {
        "standard" => PricingPolicies.Standard(order.DistanceKm),
        "express" => PricingPolicies.Express(order.DistanceKm),
        _ => throw new InvalidOperationException("Validated order has an unsupported category.")
    };
}
