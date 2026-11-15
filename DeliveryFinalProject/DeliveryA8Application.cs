namespace DeliveryFinalProject;

public sealed record NativeScoreRow(string OrderId, int Score);
public sealed record DeliveryA8Result(
    DeliverySummary Summary,
    IReadOnlyList<OrderReportRow> ExpressOrders,
    IReadOnlyList<CategoryReportRow> CategorySummary,
    IReadOnlyList<NativeScoreRow> NativeScores,
    decimal ReportTotalCost);

public static class DeliveryA8Application
{
    public static Result<DeliveryA8Result> ProcessFile(
        string path, INativeScoreCalculator nativeCalculator)
    {
        ArgumentNullException.ThrowIfNull(nativeCalculator);

        return DeliveryApplication.ProcessFile(path)
            .Bind(summary => BuildResult(summary, nativeCalculator));
    }

    public static Result<DeliveryA8Result> BuildResult(
        DeliverySummary summary, INativeScoreCalculator nativeCalculator)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(nativeCalculator);

        var nativeScores = new List<NativeScoreRow>();
        foreach (var quote in summary.Quotes)
        {
            var nativeResult = nativeCalculator.Calculate(quote);
            if (nativeResult.IsFailure)
                return Result<DeliveryA8Result>.Failure(
                    $"Native score failed for order {quote.OrderId}: {nativeResult.Error}");

            nativeScores.Add(new NativeScoreRow(quote.OrderId, nativeResult.Value));
        }

        var expressReport = DeliveryReports.GetExpressOrders(summary.Quotes);
        var categoryReport = DeliveryReports.GetCategorySummary(summary.Quotes);

        return Result<DeliveryA8Result>.Success(new DeliveryA8Result(
            summary,
            expressReport,
            categoryReport,
            Array.AsReadOnly(nativeScores.ToArray()),
            DeliveryReports.CalculateTotal(summary.Quotes)));
    }

    public static Result<bool> ExportCategoryReport(
        IReadOnlyList<CategoryReportRow> report, string path)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (string.IsNullOrWhiteSpace(path))
            return Result<bool>.Failure("Report output path is required.");

        try
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var lines = new List<string> { "Category,OrderCount,TotalCost,AverageCost" };
            lines.AddRange(report.Select(row =>
                $"{row.Category},{row.OrderCount},{row.TotalCost},{row.AverageCost}"));
            File.WriteAllLines(fullPath, lines);
            return Result<bool>.Success(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return Result<bool>.Failure($"Could not export category report: {ex.Message}");
        }
    }
}
