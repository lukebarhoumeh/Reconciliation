# Reconciliation
MSP HUB RECONCILATION TOOL
\n## CSV Normalization\nThe application now uses `CsvNormalizer` to clean imported CSV files and `ErrorLogger` to store parsing errors.
\n### Running Tests\nUse `dotnet test Reconciliation.Tests/Reconciliation.Tests.csproj`.
\n### New Features\n- Fuzzy column matching with optional checkbox in the UI.\n- Human-friendly error logs with timestamps.
