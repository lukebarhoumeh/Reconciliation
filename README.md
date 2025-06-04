# Reconciliation
MSP HUB RECONCILATION TOOL

## CSV Normalization
The application now uses `CsvNormalizer` to clean imported CSV files and `ErrorLogger` to store parsing errors and warnings. Validation errors are exported as structured CSV files for easy review.

### Running Tests
Use `dotnet test Reconciliation.Tests/Reconciliation.Tests.csproj`.

### New Features
- Fuzzy column matching with optional checkbox in the UI.
- Human-friendly error and warning logs with timestamps and summaries.
- Discrepancy detector with numeric/date tolerance and fuzzy text comparison.

### Discrepancy Detection
`DiscrepancyDetector` compares two tables and groups any differences. Adjust the
tolerances by setting `NumericTolerance`, `DateTolerance` and `TextDistance` on
the detector instance.

Example summary output:

```
Total rows compared: 2
Discrepancies found: 1
1 rows: Numeric mismatch in Price: 10 vs 10.5
```

After running a comparison you can export results:

```csharp
var detector = new DiscrepancyDetector();
detector.Compare(tableA, tableB);
detector.ExportCsv("diff.csv");
File.WriteAllText("summary.txt", detector.GetSummary());
```

`diff.csv` will contain rows with the left and right values plus a reason for
each discrepancy.
