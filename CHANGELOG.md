# Changelog

## Unreleased
- Implemented structured error logging with CSV export including row numbers and context.
- Added DataQualityValidator for comprehensive invoice checks.
- Updated UI to use new ErrorLogger.Entries collection instead of removed Errors property.
- Enforced required schema columns for Microsoft and MSP Hub invoices.
- Flexible schema validation now auto-maps common column variants (e.g. `SkuName` to `SkuId`) and suggests alternatives when a column is missing.
- Validation tolerances now loaded from `appsettings.json` and expanded fuzzy column mapping.
- Error log deduplication with configurable summary rows.
- Raw value logging now captures only the offending cell content.
- Context values always include customer and SKU or `(missing)` placeholders.
- Extracted comparison logic into `ReconciliationService` with dedicated tests.
- Added `PriceMismatchService` for price difference detection and Excel export.
- Fixed crash when highlighting results with missing "Reason" column by always creating it and skipping highlight when absent.
