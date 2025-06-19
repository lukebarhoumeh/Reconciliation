# Changelog

## Unreleased
- Accountant-friendly reconciliation outputs with mapped field names, grouped summaries and suggested actions.
- Implemented structured error logging with CSV export including row numbers and context.
- Added DataQualityValidator for comprehensive invoice checks.
- Updated UI to use new ErrorLogger.Entries collection instead of removed Errors property.
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
- Partner discount validation rounds to one decimal place to allow 20.0–20.1%.
- Results no longer auto-switch to the Logs tab; the tab header flashes instead.
- Numeric formatter extracted and applied to discrepancy values.
- Partner discount validation rounds values before comparison and accepts up to 20.1%.
- Exported logs and grids show money and percentages with at most two decimals.
