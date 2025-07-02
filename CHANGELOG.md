# Changelog

## Unreleased
- Business-key reconciliation uses alias mapping and logs kid-friendly summary buckets.
- Tenant slice and alias fixes ensure Microsoft rows are filtered to the MSP tenant and SubscriptionGUID is recognised.
- Updated tests to target only `net8.0` and fixed build on non-Windows hosts.
- Removed leftover `RowPrePaint` event wiring in `Form1`.
- Missing-row detection no longer exits early and tenant filtering ignores case.
- Pinned SDK version to `8.0.117` for Linux CI compatibility.
- Reconciliation builds as a WinForms executable (`Reconciliation.exe`).
- Row matching now uses only `CustomerDomainName` and `ProductId` so missing columns no longer skip rows.
- Added advanced reconciliation service with composite key grouping and mapping
  via `column-map.json`.
- Added `BusinessKeyReconciliationService` for strict business-key matching and
  financial column comparison with tolerance.
- Replaced legacy compare logic with `BusinessKeyReconciliationService`, simplified UI filters and per-run CSV logging.
- Improved button labels, tooltips and grid styling in the main UI for clarity.
- Invoice validation exposes `InvalidRowsView` for easier binding and tests now reference the UI project.
- UI option "Advanced Compare" runs the new engine and shows summary details.
- Validation results now report counts of high and low priority errors with a filter option.
- Credit and debit rows with the same customer and SKU are now netted before
  calculating price mismatches.
- Removed ComboBox.PlaceholderText usage and added a selectable "-- Select a Field --" option for compatibility with older WinForms versions.
- Expression mapping with `{Column}` syntax and per-day rounding tolerance.
- Composite key list expanded with OrderId and InvoiceNumber; dates normalised to `yyyy-MM-dd`.
- Accountant-friendly reconciliation outputs with mapped field names, grouped summaries and suggested actions.
- Filter dropdown and tooltips guide accountants; summaries clear on mode switch and exports include top-level summary row.
- Implemented structured error logging with CSV export including row numbers and context.
- Added DataQualityValidator for comprehensive invoice checks.
- Updated UI to use new ErrorLogger.Entries collection instead of removed Errors property.
- Modernised WinForms interface with responsive layout and theme.
- Enforced required schema columns for Microsoft and MSP Hub invoices.
- Flexible schema validation now auto-maps common column variants (e.g. `SkuName` to `SkuId`) and suggests alternatives when a column is missing.
- MSP Hub and Microsoft logos are embedded via resources and shown in the main form.
- Validation tolerances now loaded from `appsettings.json` and expanded fuzzy column mapping.
- Error log deduplication with configurable summary rows.
- Raw value logging now captures only the offending cell content.
- Context values always include customer and SKU or `(missing)` placeholders.
- Extracted comparison logic into `ReconciliationService` with dedicated tests.
- Added `PriceMismatchService` for price difference detection and Excel export.
- Fixed crash when highlighting results with missing "Reason" column by always creating it and skipping highlight when absent.
- MSP Hub import handles `SkuName` column when fuzzy matching is enabled.
- Updated `global.json` to target .NET SDK 8.0.100 with roll-forward to latest patch.
- Partner discount validation rounds to one decimal place to allow 20.0–20.1%.
- Results no longer auto-switch to the Logs tab; the tab header flashes instead.
- Numeric formatter extracted and applied to discrepancy values.
- Partner discount validation rounds values before comparison and accepts up to 20.1%.
- Exported logs and grids show money and percentages with at most two decimals.
- Test project multi-targets `net8.0` and `net8.0-windows` to avoid NU1201 when referencing the UI project.
