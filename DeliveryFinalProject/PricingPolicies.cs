namespace DeliveryFinalProject;

public static class PricingPolicies
{
    public static decimal Standard(decimal distanceKm) => 500m + 100m * distanceKm;
    public static decimal Express(decimal distanceKm) => 800m + 150m * distanceKm;
}
