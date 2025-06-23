# Reconciliation
MSP HUB RECONCILATION TOOL

The UI now embeds MSP Hub and Microsoft logos directly from application resources.

## CSV Normalization
The application now uses `CsvNormalizer` to clean imported CSV files and `ErrorLogger` to store parsing errors and warnings. Validation errors are exported as structured CSV files for easy review.

### Required Columns
Both invoice types are validated after import. All required headers must match exactly.
If any column is missing the import will fail and list the missing names.

- **Microsoft invoice** must include: `CustomerDomainName`, `ProductId`, `SkuId`, `ChargeType`, `TermAndBillingCycle`.
- **MSP Hub invoice** must include: `InternalReferenceId`, `SkuId`, `BillingCycle`.

Sample templates are available under `samples/`. Ensure the headers match the following names exactly.

### Running Tests
Use `dotnet test Reconciliation.Tests/Reconciliation.Tests.csproj`.

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
can be unit tested without the WinForms UI. Use
`CompareInvoices(msphub, microsoft)` to get a table of discrepancies.

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

## Advanced usage
Use the **Export** menu to save error logs or comparison results.


