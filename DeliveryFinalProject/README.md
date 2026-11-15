# Delivery Management System — Final Project

**Student:** Yerassyl Yergaliyev  
**Group:** IT-2505  
**Course:** Introduction to Functional Programming  
**Application:** C# / .NET 9 console application  
**Project type:** Individual

## 1. Problem and purpose

The application processes delivery orders from a JSON file. It validates order data, calculates Standard or Express delivery prices, rejects invalid orders, supports a safe approval-and-dispatch lifecycle, finds individual orders, and creates CSV reports.

## 2. Main workflows

1. **Batch processing:** load orders, validate them, calculate quotes, and build a summary.
2. **Order lookup:** search for a quote by order ID and represent a missing order with `Option<T>`.
3. **Approve and dispatch:** validate an order, price it, require payment confirmation, and require a tracking code before dispatch.
4. **Reporting:** create an Express-orders report and a category summary, then export the category report to CSV.
5. **Save order data:** serialize an order collection to JSON through the file boundary.

## 3. Architecture and functional design

- `DeliveryOrder.cs` — immutable request and validated-order models; validation is performed by a factory.
- `DeliveryQuote.cs` — immutable quote, summary, and report data models.
- `DeliveryPipeline.cs` — three named stages: `ValidateOrders`, `CalculateQuotes`, and `BuildSummary`. Each stage receives the previous stage's output.
- `DeliveryLifecycle.cs` — safe state transitions. `PricedDeliveryOrder`, `ApprovedDeliveryOrder`, and `DispatchedDeliveryOrder` have private constructors. Dispatch requires an `ApprovedDeliveryOrder`, so an unapproved order cannot be passed to the dispatch operation.
- `Result.cs` and `Option.cs` — explicit success/failure and found/not-found models. `Map` and `Bind` compose dependent operations and skip later work after a failure.
- `PricingPolicies.cs` and `Scoring.cs` — configurable pricing and higher-order scoring functions. The pipeline accepts a `Func<ValidatedDeliveryOrder, decimal>` pricing rule that changes the calculated business price.
- `Extensions.cs` — custom `ForCategory` and `TotalCost` extension methods.
- `DeliveryReports.cs` — two LINQ reports. The reports use filtering, projection, ordering with tie-breakers, `GroupBy`, counting, summing, and averaging.
- `DeferredProcessing.cs` — lazy `IEnumerable` processing with `yield return`; `CreateSnapshot` deliberately materializes a reusable read-only snapshot.
- `DeliveryFileService.cs` — JSON load/save boundary with explicit failures for missing files and malformed JSON.
- `DeliveryA8Application.cs` — application composition, native scoring adapter integration, and CSV report export.
- `Native/` and `NativeMethods.cs` — optional C scoring library called through P/Invoke.
- `Tests/` — xUnit automated tests.

Pure validation, pricing, state transitions, transformations, and report calculations do not read from the console or access files. File and console operations stay at the application boundaries.

## 4. Error handling

Domain failures include missing order IDs, invalid or negative distances, unsupported delivery categories, unconfirmed payment, missing tracking codes, and invalid pricing results. `Result<T>` carries success or failure explicitly. `Option<T>` represents an order lookup that may not find a match. File operations catch relevant I/O, path, access, and JSON exceptions and return clear failure results.

## 5. Sample data and expected output

The sample file is `SampleData/orders.json`. It contains six requests: three valid and three invalid. The expected valid delivery total is **2750 KZT**; three orders are rejected. The application exports `Reports/category-summary.csv`.

## 6. Requirements mapping

| Final requirement | Implementation |
|---|---|
| At least two domain models | `DeliveryOrderRequest`, `ValidatedDeliveryOrder`, `DeliveryQuote`, lifecycle models |
| Immutable core models | Records and read-only properties/collections |
| Pattern matching | `DeliveryPipeline.CalculateQuote`, `DefaultPricingRule`, lifecycle state handling |
| Three-stage pipeline | `ValidateOrders` → `CalculateQuotes` → `BuildSummary` |
| Higher-order business rule | `DeliveryPipeline.Run(..., Func<ValidatedDeliveryOrder, decimal>)` |
| Safe operation ordering | `PricedDeliveryOrder` → `ApprovedDeliveryOrder` → `DispatchedDeliveryOrder` |
| Explicit failures and `Map`/`Bind` | `Result<T>`, `DeliveryApplication`, `DeliveryOrderWorkflow` |
| Missing lookup model | `Option<DeliveryQuote>` in `FindQuote` |
| Two extension methods | `ForCategory`, `TotalCost` |
| Two LINQ reports | `GetExpressOrders`, `GetCategorySummary` |
| Lazy processing | `StreamValidQuotes`, `CreateSnapshot` |
| JSON persistence and report export | `DeliveryFileService`, `ExportCategoryReport` |
| Automated tests | xUnit tests in `Tests/` |

## 7. Build, run, and test

### Requirements

- .NET 9 SDK
- Visual Studio 2022 or another .NET 9 compatible IDE
- Windows x64 or Linux x64 for the included native library build

### Commands

Run these commands from the project root:

```bash
dotnet restore DeliveryFinalProject.csproj
dotnet build DeliveryFinalProject.csproj
dotnet run --project DeliveryFinalProject.csproj
dotnet test Tests/DeliveryFinalProject.Tests.csproj
```

To process another JSON file, pass its path:

```bash
dotnet run --project DeliveryFinalProject.csproj -- SampleData/orders.json
```

The sample data is copied to the build output directory. On Windows, the included `Native/delivery_native.dll` is used. To rebuild the native library, open an x64 Native Tools Command Prompt for Visual Studio and run `Native/build-windows.bat`. The native library is an additional additional native scoring feature; the functional delivery pipeline and reports are implemented in managed C#.

## 8. Test coverage

The source currently contains 50 xUnit `[Fact]` tests. They cover validation, pricing, boundaries, pattern matching, pipeline stages, failure propagation, missing lookups, lifecycle ordering, extension methods, LINQ reports, deferred execution, JSON loading/saving, missing files, malformed JSON, CSV export, and the native adapter. Run the test command above to obtain the actual pass/fail summary on the target machine; the source count is not a substitute for a successful test run.

## 9. Repository details



- **Repository URL:https://github.com/Yerassyl525/Delivery-Management-System.git**
- branch :main
coomit : 6387c27



## 10. Limitations

The project is a console application and uses a local JSON file rather than a database. It does not include user authentication, cloud deployment, external APIs, or a graphical interface. Native scoring requires a compatible platform-specific library; the core delivery logic does not depend on native scoring.

## 11. External libraries

The test project uses xUnit 2.9.2, xUnit Visual Studio runner 2.8.2, and Microsoft.NET.Test.Sdk 17.12.0. The application uses the .NET 9 base class libraries and a small native C scoring example included in the repository.
