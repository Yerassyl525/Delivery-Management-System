namespace DeliveryFinalProject;

public sealed record DeliveryQuote(string OrderId, decimal DistanceKm, string Category, decimal Cost);

public sealed record DeliverySummary(
    IReadOnlyList<DeliveryQuote> Quotes,
    decimal TotalCost,
    int RejectedOrders);
