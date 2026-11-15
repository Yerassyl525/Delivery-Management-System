using System.Globalization;

namespace DeliveryFinalProject;

public sealed record DeliveryOrderRequest(string? Id, string? DistanceText, string? Category);

public abstract record DeliveryOrderValidation
{
    private DeliveryOrderValidation() { }

    public sealed record Valid(ValidatedDeliveryOrder Order) : DeliveryOrderValidation;
    public sealed record Invalid(IReadOnlyList<string> Errors) : DeliveryOrderValidation;
}

public sealed record ValidatedDeliveryOrder
{
    private ValidatedDeliveryOrder(string id, decimal distanceKm, string category)
    {
        Id = id;
        DistanceKm = distanceKm;
        Category = category;
    }

    public string Id { get; }
    public decimal DistanceKm { get; }
    public string Category { get; }

    public static DeliveryOrderValidation Create(string? id, string? distanceText, string? category)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(id))
            errors.Add("Order ID is required.");

        decimal distance = 0;
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            errors.Add("Distance is required.");
        }
        else if (!decimal.TryParse(distanceText, NumberStyles.Number, CultureInfo.InvariantCulture, out distance))
        {
            errors.Add("Distance must be a valid number.");
        }
        else if (distance < 0)
        {
            errors.Add("Distance cannot be negative.");
        }

        var normalizedCategory = category?.Trim().ToLowerInvariant();
        if (normalizedCategory is not ("standard" or "express"))
            errors.Add("Category must be Standard or Express.");

        if (errors.Count > 0)
            return new DeliveryOrderValidation.Invalid(Array.AsReadOnly(errors.ToArray()));

        return new DeliveryOrderValidation.Valid(
            new ValidatedDeliveryOrder(id!.Trim(), distance, normalizedCategory!));
    }
}
