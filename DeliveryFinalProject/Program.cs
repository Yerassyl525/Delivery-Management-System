using DeliveryFinalProject;

var inputPath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "SampleData", "orders.json");

Console.WriteLine("DELIVERY MANAGEMENT SYSTEM — FINAL PROJECT");
var processing = DeliveryA8Application.ProcessFile(inputPath, new NativeScoreCalculator());

var exitCode = processing.Match(
    result =>
    {
        var summary = result.Summary;
        Console.WriteLine($"Processed: {summary.Quotes.Count + summary.RejectedOrders}");
        Console.WriteLine($"Valid: {summary.Quotes.Count}; rejected: {summary.RejectedOrders}");
        Console.WriteLine($"Total delivery cost: {summary.TotalCost} KZT");
        Console.WriteLine($"Report extension total: {result.ReportTotalCost} KZT");

        Console.WriteLine("\nREPORT 1: EXPRESS ORDERS");
        foreach (var row in result.ExpressOrders)
            Console.WriteLine($"{row.OrderId}: {row.DistanceKm} km, {row.Cost} KZT");

        Console.WriteLine("\nREPORT 2: TOTALS BY CATEGORY");
        foreach (var row in result.CategorySummary)
            Console.WriteLine($"{row.Category}: {row.OrderCount} orders, {row.TotalCost} KZT, average {row.AverageCost}");

        Console.WriteLine("\nNATIVE C SCORES");
        foreach (var row in result.NativeScores)
            Console.WriteLine($"{row.OrderId}: score {row.Score}");

        Console.WriteLine("\nCURRIED SCORE CONFIGURATIONS");
        var standardScores = DeliveryScoring.BuildReport(summary.Quotes, DeliveryScoring.StandardConfiguration);
        var priorityScores = DeliveryScoring.BuildReport(summary.Quotes, DeliveryScoring.PriorityConfiguration);
        foreach (var row in standardScores)
            Console.WriteLine($"Standard config {row.OrderId}: {row.Score}");
        foreach (var row in priorityScores)
            Console.WriteLine($"Priority config {row.OrderId}: {row.Score}");

        var export = DeliveryA8Application.ExportCategoryReport(
            result.CategorySummary, Path.Combine("Reports", "category-summary.csv"));
        export.Match(
            _ => { Console.WriteLine("\nCategory report exported to Reports/category-summary.csv"); return 0; },
            error => { Console.WriteLine($"\nEXPORT FAILED: {error}"); return 0; });

        Console.WriteLine("\nWORKFLOW 2: FIND ORDER");
        DeliveryApplication.FindQuote(summary, "A").Match(
            quote => { Console.WriteLine($"Found order A: {quote.Cost} KZT"); return 0; },
            () => { Console.WriteLine("Order A was not found."); return 0; });

        Console.WriteLine("\nWORKFLOW 3: APPROVE AND DISPATCH");
        var dispatch = DeliveryOrderWorkflow.ApproveAndDispatch(
            new DeliveryOrderRequest("LIVE-1", "4", "Express"), true, "TRK-2026-001");
        dispatch.Match(
            order => { Console.WriteLine($"Dispatched {order.ApprovedOrder.PricedOrder.Order.Id} with tracking {order.TrackingCode}"); return 0; },
            error => { Console.WriteLine($"DISPATCH FAILED: {error}"); return 0; });

        var rejectedDispatch = DeliveryOrderWorkflow.ApproveAndDispatch(
            new DeliveryOrderRequest("LIVE-2", "3", "Standard"), false, "TRK-2026-002");
        rejectedDispatch.Match(
            _ => { Console.WriteLine("Unexpected approval."); return 0; },
            error => { Console.WriteLine($"Expected rejection: {error}"); return 0; });
        return 0;
    },
    error =>
    {
        Console.WriteLine($"PROCESSING FAILED: {error}");
        return 1;
    });

Console.WriteLine("\nDEFERRED EXECUTION DEMONSTRATION");
var processedCount = 0;
IEnumerable<DeliveryOrderRequest> Probe()
{
    processedCount++;
    yield return new DeliveryOrderRequest("LAZY-1", "3", "Standard");
}
var deferred = DeferredProcessing.StreamValidQuotes(Probe());
Console.WriteLine($"Before enumeration: {processedCount}");
var snapshot = deferred.ToArray();
Console.WriteLine($"After materialisation: {processedCount}; snapshot count: {snapshot.Length}");
Console.WriteLine($"Repeated snapshot use: {snapshot.Length}");

return exitCode;
