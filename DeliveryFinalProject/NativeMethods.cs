using System.Runtime.InteropServices;

namespace DeliveryFinalProject;

public interface INativeScoreCalculator
{
    Result<int> Calculate(DeliveryQuote quote);
}

public static class NativeMethods
{
    [DllImport("delivery_native", EntryPoint = "calculate_delivery_score", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int CalculateDeliveryScore(double distanceKm, int isExpress);
}

public sealed class NativeScoreCalculator : INativeScoreCalculator
{
    public Result<int> Calculate(DeliveryQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);

        if (quote.DistanceKm < 0m || quote.DistanceKm > 1000m)
            return Result<int>.Failure("Native score distance must be between 0 and 1000 km.");

        var isExpress = string.Equals(quote.Category, "standard", StringComparison.OrdinalIgnoreCase)
            ? 0
            : string.Equals(quote.Category, "express", StringComparison.OrdinalIgnoreCase) ? 1 : -1;

        if (isExpress < 0)
            return Result<int>.Failure("Native score category must be Standard or Express.");

        try
        {
            var score = NativeMethods.CalculateDeliveryScore((double)quote.DistanceKm, isExpress);
            if (score < 0)
                return Result<int>.Failure("Native library rejected the supplied values.");
            return Result<int>.Success(score);
        }
        catch (DllNotFoundException)
        {
            return Result<int>.Failure("Native library 'delivery_native' was not found. Build and copy the native library first.");
        }
        catch (EntryPointNotFoundException)
        {
            return Result<int>.Failure("Native function calculate_delivery_score was not found.");
        }
        catch (BadImageFormatException)
        {
            return Result<int>.Failure("Native library architecture does not match the application architecture.");
        }
    }
}
