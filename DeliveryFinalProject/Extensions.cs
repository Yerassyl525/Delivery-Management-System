namespace DeliveryFinalProject;

public static class DeliveryExtensions
{
    public static IEnumerable<DeliveryQuote> ForCategory(
        this IEnumerable<DeliveryQuote> quotes, string category)
    {
        ArgumentNullException.ThrowIfNull(quotes);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        return quotes.Where(quote => string.Equals(
            quote.Category, category.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static decimal TotalCost(this IEnumerable<DeliveryQuote> quotes)
    {
        ArgumentNullException.ThrowIfNull(quotes);
        return quotes.Sum(quote => quote.Cost);
    }
}
