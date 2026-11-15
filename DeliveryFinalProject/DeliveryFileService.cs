using System.Text.Json;

namespace DeliveryFinalProject;

public static class DeliveryFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Result<bool> SaveRequests(string path, IReadOnlyList<DeliveryOrderRequest> requests)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result<bool>.Failure("Output file path is required.");
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Any(request => request is null))
            return Result<bool>.Failure("Order collection cannot contain null entries.");

        try
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(requests, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fullPath, json);
            return Result<bool>.Success(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Result<bool>.Failure($"Could not save delivery orders: {ex.Message}");
        }
    }

    public static Result<IReadOnlyList<DeliveryOrderRequest>> LoadRequests(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result<IReadOnlyList<DeliveryOrderRequest>>.Failure("Input file path is required.");

        try
        {
            var json = File.ReadAllText(path);
            var requests = JsonSerializer.Deserialize<List<DeliveryOrderRequest>>(json, JsonOptions);

            if (requests is null)
                return Result<IReadOnlyList<DeliveryOrderRequest>>.Failure("JSON must contain an array of delivery orders.");

            if (requests.Any(request => request is null))
                return Result<IReadOnlyList<DeliveryOrderRequest>>.Failure("JSON contains a null order entry.");

            return Result<IReadOnlyList<DeliveryOrderRequest>>.Success(requests.AsReadOnly());
        }
        catch (FileNotFoundException)
        {
            return Result<IReadOnlyList<DeliveryOrderRequest>>.Failure($"Input file was not found: {path}");
        }
        catch (DirectoryNotFoundException)
        {
            return Result<IReadOnlyList<DeliveryOrderRequest>>.Failure($"Input directory was not found: {path}");
        }
        catch (UnauthorizedAccessException)
        {
            return Result<IReadOnlyList<DeliveryOrderRequest>>.Failure("Access to the input file was denied.");
        }
        catch (IOException ex)
        {
            return Result<IReadOnlyList<DeliveryOrderRequest>>.Failure($"Could not read input file: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return Result<IReadOnlyList<DeliveryOrderRequest>>.Failure($"Invalid input file path: {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            return Result<IReadOnlyList<DeliveryOrderRequest>>.Failure($"Unsupported input file path: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return Result<IReadOnlyList<DeliveryOrderRequest>>.Failure($"Input JSON is malformed: {ex.Message}");
        }
    }
}
