namespace DeliveryFinalProject;

public static class DeliveryApplication
{
    public static Result<DeliverySummary> ProcessFile(string path) =>
        DeliveryFileService.LoadRequests(path)
            .Bind(EnsureRequestsExist)
            .Map(DeliveryPipeline.Run);

    public static Option<DeliveryQuote> FindQuote(DeliverySummary summary, string orderId)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var quote = summary.Quotes.FirstOrDefault(item =>
            string.Equals(item.OrderId, orderId, StringComparison.OrdinalIgnoreCase));
        return Option<DeliveryQuote>.FromNullable(quote);
    }

    private static Result<IReadOnlyList<DeliveryOrderRequest>> EnsureRequestsExist(
        IReadOnlyList<DeliveryOrderRequest> requests) =>
        requests.Count == 0
            ? Result<IReadOnlyList<DeliveryOrderRequest>>.Failure("Input file contains no delivery orders.")
            : Result<IReadOnlyList<DeliveryOrderRequest>>.Success(requests);
}
