namespace DeliveryFinalProject;

public static class DeferredProcessing
{
    public static IEnumerable<DeliveryQuote> StreamValidQuotes(
        IEnumerable<DeliveryOrderRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        foreach (var request in requests)
        {
            var validation = ValidatedDeliveryOrder.Create(
                request.Id, request.DistanceText, request.Category);
            var result = DeliveryPipeline.CalculateQuote(validation);

            if (result.IsSuccess)
                yield return result.Value;
        }
    }

    public static IReadOnlyList<DeliveryQuote> CreateSnapshot(
        IEnumerable<DeliveryOrderRequest> requests) =>
        Array.AsReadOnly(StreamValidQuotes(requests).ToArray());
}
