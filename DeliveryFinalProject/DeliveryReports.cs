namespace DeliveryFinalProject;

public sealed record OrderReportRow(string OrderId, string Category, decimal DistanceKm, decimal Cost);
public sealed record CategoryReportRow(string Category, int OrderCount, decimal TotalCost, decimal AverageCost);

public static class DeliveryReports
{
    public static IReadOnlyList<OrderReportRow> GetExpressOrders(IEnumerable<DeliveryQuote> quotes)
    {
        ArgumentNullException.ThrowIfNull(quotes);

        var rows = quotes
            .ForCategory("express")
            .Where(quote => quote.DistanceKm >= 0)
            .Select(quote => new OrderReportRow(
                quote.OrderId, quote.Category, quote.DistanceKm, quote.Cost))
            .OrderByDescending(row => row.Cost)
            .ThenBy(row => row.OrderId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Array.AsReadOnly(rows);
    }

    public static IReadOnlyList<CategoryReportRow> GetCategorySummary(IEnumerable<DeliveryQuote> quotes)
    {
        ArgumentNullException.ThrowIfNull(quotes);

        var rows = quotes
            .GroupBy(quote => quote.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CategoryReportRow(
                group.Key,
                group.Count(),
                group.Sum(quote => quote.Cost),
                group.Average(quote => quote.Cost)))
            .OrderByDescending(row => row.TotalCost)
            .ThenBy(row => row.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Array.AsReadOnly(rows);
    }

    public static decimal CalculateTotal(IEnumerable<DeliveryQuote> quotes)
    {
        ArgumentNullException.ThrowIfNull(quotes);
        return quotes.TotalCost();
    }
}
