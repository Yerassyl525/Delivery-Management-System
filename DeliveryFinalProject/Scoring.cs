namespace DeliveryFinalProject;

public static class DeliveryScoring
{
    public static Func<decimal, Func<DeliveryQuote, decimal>> CreateScore(decimal distanceWeight) =>
        expressBonus => quote =>
            quote.DistanceKm * distanceWeight +
            (string.Equals(quote.Category, "express", StringComparison.OrdinalIgnoreCase)
                ? expressBonus
                : 0m);

    public static Func<DeliveryQuote, decimal> StandardConfiguration =>
        CreateScore(10m)(50m);

    public static Func<DeliveryQuote, decimal> PriorityConfiguration =>
        CreateScore(20m)(100m);

    public static IReadOnlyList<(string OrderId, decimal Score)> BuildReport(
        IEnumerable<DeliveryQuote> quotes,
        Func<DeliveryQuote, decimal> score)
    {
        ArgumentNullException.ThrowIfNull(quotes);
        ArgumentNullException.ThrowIfNull(score);

        return Array.AsReadOnly(quotes
            .Select(quote => (quote.OrderId, score(quote)))
            .OrderByDescending(item => item.Item2)
            .ThenBy(item => item.OrderId, StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }
}
