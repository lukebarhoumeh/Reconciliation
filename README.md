# Reconciliation
MSP HUB RECONCILATION TOOL ![CI](https://github.com/atlas/unknown/actions/workflows/dotnet.yml/badge.svg)

## Build
Requires .NET 8 SDK. Restore and build the solution:

```bash
dotnet build "Reconciliation Tool.sln"
```

Run the tests:

```bash
dotnet test Reconciliation.Tests/Reconciliation.Tests.csproj
```

## Usage
Start the Windows GUI or use the CLI for automation. Sample files live under `samples/`.

### CLI
```bash
dotnet run --project Reconciliation.Cli -- --left microsoft.csv --right msphub.csv \
    --output-diff diff.csv --output-log errors.csv
```

### GUI
```bash
Reconciliation.exe microsoft.csv msphub.csv
```

Adjust tolerances in `Reconciliation/appsettings.json` if default numeric or date thresholds do not match your workflow. The `Logging` section controls the number of detailed error rows kept before a summary line is written.

## CSV Normalization
The application now uses `CsvNormalizer` to clean imported CSV files and `ErrorLogger` to store parsing errors and warnings. Validation errors are exported as structured CSV files for easy review.

### Required Columns
Both invoice types are validated after import. Missing columns are automatically
matched against common variations and renamed when possible. Blocking errors are
only shown if no suitable column can be mapped.

- **Microsoft invoice** must include: `CustomerDomainName`, `ProductId`, `SkuId`, `ChargeType`, `TermAndBillingCycle`.
- **MSP Hub invoice** must include: `InternalReferenceId`, `SkuId`, `BillingCycle`.

Sample templates are available under `samples/`.

### Running Tests
Use `dotnet test Reconciliation.Tests/Reconciliation.Tests.csproj`.

Test results are written to the `TestResults` directory by the CI workflow and
uploaded as an artifact when available. If no results are produced the workflow
logs a warning rather than failing the build.
See the workflow run summary for links to download artifacts.

- Fuzzy column matching with optional checkbox in the UI. Common variants such
  as `SkuName`, `SKU` or `sku_id` are automatically mapped to `SkuId` during
  import.
- Human-friendly error and warning logs with timestamps and summaries.
- Repeated parsing errors are summarised after five occurrences to reduce noise.
- Data quality warnings highlight when more than 10% of rows contain blank or zero values in critical fields.
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
## Performance
Streaming CSV reading is available via `CsvNormalizer.StreamCsv` for large files. Current normalization loads files into memory; future work will refactor to use full streaming.


## Advanced usage
Use the **Export** menu to save error logs or comparison results. Add additional fuzzy column variants by editing `DataTableExtensions.ColumnVariants`.

## Troubleshooting
- Ensure CSV files are UTF-8 with comma delimiters.
- Missing rows are often caused by mismatched column headers – check the mappings and adjust `appsettings.json` thresholds if needed.
