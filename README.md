# Reconciliation
MSP HUB RECONCILATION TOOL

The UI now embeds MSP Hub and Microsoft logos directly from application resources.
The WinForms interface uses a modern layout with responsive tables and large buttons for 4K displays.

## CSV Normalization
The application now uses `CsvNormalizer` to clean imported CSV files and `ErrorLogger` to store parsing errors and warnings. Validation errors are exported as structured CSV files for easy review.

### Required Columns
Both invoice types are validated after import. All required headers must match exactly.
If any column is missing the import will fail and list the missing names.

- **Microsoft invoice** must include: `CustomerDomainName`, `ProductId`, `SkuId`, `ChargeType`, `TermAndBillingCycle`.
- **MSP Hub invoice** must include: `InternalReferenceId`, `SkuId`, `BillingCycle`.

Sample templates are available under `samples/`. Ensure the headers match the following names exactly.

### Running Tests
Use `dotnet test Reconciliation.Tests/Reconciliation.Tests.csproj` to run tests targeting `net8.0`.

### Building the WinForms application
The project now targets `net8.0-windows` and builds `Reconciliation.exe`.
After running `dotnet build`, check `bin/Debug` for the generated executable.

- Human-friendly error and warning logs with timestamps and summaries.
- Logs tab header flashes red for five seconds when new entries are added.
- Repeated parsing errors are summarised after five occurrences to reduce noise.
- Money and percent values display with at most two decimals.
- Data quality warnings highlight when more than 10% of rows contain blank or zero values in critical fields.
- Discrepancy detector with numeric and date tolerances.

### Discrepancy Detection
`DiscrepancyDetector` compares two tables and groups any differences. Adjust the
tolerances by setting `NumericTolerance`, `DateTolerance` and `TextDistance` on
the detector instance.

`ReconciliationService` encapsulates the external invoice matching logic so it
can be unit tested without the WinForms UI.
`BusinessKeyReconciliationService` provides stricter business-key matching and
financial comparison. Column aliases like `DomainUrl` or `SubscriptionGuid`
are normalised automatically and the summary logs now show kid-friendly
counts of perfect matches, missing rows and mismatches.

```csharp
var svc = new BusinessKeyReconciliationService(
    new[]{"CustomerDomainName","ProductId","ChargeType","ChargeStartDate","SubscriptionId"});
var result = svc.Reconcile(msphub, microsoft);
```

### Invoice Validation
`InvoiceValidationService.ValidateInvoice` now returns an `InvoiceValidationResult`
with an `InvalidRowsView` property for easy data binding in the UI or tests.

`PriceMismatchService` detects unit price differences between the two invoices
and can export the mismatches to Excel. Credit lines followed by a matching
debit are aggregated so prorated adjustments cancel out before comparison.

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

Discrepancies are ordered by row number and field name in both the on-screen
table and the exported CSV so the results are easier to scan.

## Advanced usage
Use the **Export** menu to save error logs or comparison results.
The new reconciliation engine loads mapping rules from `column-map.json` and
groups rows by a composite key to detect genuine billing differences.
Choose **Advanced Compare** in the UI to enable this engine.

### Expression mapping
Derived numeric columns in the map can use arithmetic expressions. Place column
names inside curly braces, e.g. `{UnitPrice} * {Quantity}`. Expressions are
evaluated per row before normalisation.

Composite keys are configured in `appsettings.json`. Add or remove column names
to match how your data uniquely identifies invoice lines.



