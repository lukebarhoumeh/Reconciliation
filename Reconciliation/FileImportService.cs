using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Reconciliation
{
    /// <summary>
    /// Handles all CSV ingestion, normalisation and schema checks
    /// for Microsoft and MSP‑Hub invoice files.
    /// </summary>
    public class FileImportService
    {
        // ─────────────────────────────────────────────────────────────
        // 1.  CONFIGURATION CONSTANTS
        // ─────────────────────────────────────────────────────────────

        private static readonly string[] UniqueKeyColumns =
        {
            "CustomerDomainName", "ProductId", "SkuId", "ChargeType",
            "Term", "BillingCycle", "ReferenceId"
        };

        private static readonly string[] RequiredMicrosoftColumns =
        {
            "CustomerDomainName", "ProductId", "SkuId",
            "ChargeType", "TermAndBillingCycle"
        };

        private static readonly string[] RequiredMspHubColumns =
        {
            "ReferenceId", "SkuId", "BillingCycle"
        };

        /// <summary>
        /// Canonical Microsoft column order.  
        /// Extra columns from partner feeds are re‑ordered into this set
        /// so downstream code can rely on deterministic indexes.
        /// </summary>
        private static readonly string[] MicrosoftColumns =
        {
            // Core identity / customer
            "PartnerId","CustomerId","CustomerName","CustomerDomainName","CustomerCountry",
            // Commercial identifiers
            "InvoiceNumber","MpnId","Tier2MpnId","OrderId","OrderDate","ReferenceId",
            // Product identifiers
            "ProductId","SkuId","AvailabilityId","SkuName","ProductName",
            // Financials
            "ChargeType","UnitPrice","Quantity","Subtotal","TaxTotal","Total","Currency",
            "PriceAdjustmentDescription",
            // Metadata
            "PublisherName","PublisherId","SubscriptionDescription","SubscriptionId",
            "ChargeStartDate","ChargeEndDate","TermAndBillingCycle","EffectiveUnitPrice",
            "UnitType","AlternateId","BillableQuantity","BillingFrequency","PricingCurrency",
            // Added split columns the matcher expects
            "Term", "BillingCycle"
        };

        // ─────────────────────────────────────────────────────────────
        // 2.  ENTRY POINT (AUTO‑DETECT SOURCE)
        // ─────────────────────────────────────────────────────────────

        public DataTable Import(string filePath)
        {
            var table = CsvNormalizer.NormalizeCsv(filePath).Table;
            var sourceType = SourceTypeDetector.FromFilename(filePath);

            // Map aliases → canonical column names
            var normalised = CsvSchemaMapper.Normalize(table, sourceType);
            DataQualityValidator.Run(normalised, Path.GetFileName(filePath));

            // Fail fast if required headers are missing
            if (sourceType == SourceType.Microsoft)
                SchemaValidator.RequireColumns(normalised, "Microsoft invoice", RequiredMicrosoftColumns);
            else
                SchemaValidator.RequireColumns(normalised, "MSP Hub invoice", RequiredMspHubColumns);

            // Post‑processing specific to each source
            return sourceType switch
            {
                SourceType.Microsoft => PreprocessMicrosoft(normalised),
                _ => PreprocessMspHub(normalised)
            };
        }

        // Keep old public signatures so UI code compiles
        public DataView ImportMicrosoftInvoice(string filePath) =>
            PreprocessMicrosoft(CsvSchemaMapper.Normalize(
                CsvNormalizer.NormalizeCsv(filePath).Table, SourceType.Microsoft)).DefaultView;

        public DataView ImportSixDotOneInvoice(string filePath) =>
            PreprocessMspHub(CsvSchemaMapper.Normalize(
                CsvNormalizer.NormalizeCsv(filePath).Table, SourceType.Partner)).DefaultView;

        // ─────────────────────────────────────────────────────────────
        // 3.  MICROSOFT‑SPECIFIC CLEAN‑UP
        // ─────────────────────────────────────────────────────────────

        private static DataTable PreprocessMicrosoft(DataTable table)
        {
            if (table.Rows.Count == 0)
                throw new ArgumentException("The selected Microsoft file contains no rows.");

            // Drop Azure‑plan lines (business rule)
            DropRows(table, r => Safe(r, "SubscriptionDescription") == "Azure plan");

            // Split ‘TermAndBillingCycle’ if the partner feed hasn’t done it for us
            if (!table.Columns.Contains("Term") || !table.Columns.Contains("BillingCycle"))
                SplitTermAndCycle(table, "TermAndBillingCycle");

            return ReorderColumns(table, UniqueKeyColumns.Concat(new[]
                   { "TermAndBillingCycle", "BillingFrequency" }).ToArray());
        }

        // ─────────────────────────────────────────────────────────────
        // 4.  MSP‑HUB‑SPECIFIC CLEAN‑UP & MAPPING
        // ─────────────────────────────────────────────────────────────

        private static DataTable PreprocessMspHub(DataTable table)
        {
            if (table.Rows.Count == 0)
                throw new ArgumentException("The selected MSP‑Hub file contains no rows.");

            // Normalise key fields not handled by CsvSchemaMapper
            if (table.Columns.Contains("SkuId"))
                foreach (DataRow row in table.Rows)
                    row["SkuId"] = Regex.Replace(Safe(row, "SkuId"), @"^0+", ""); // trim leading zeroes

            ReplaceColumn(table, "ResourceName", "ProductName");
            ReplaceColumn(table, "ValidFrom", "ChargeStartDate");
            ReplaceColumn(table, "ValidTo", "ChargeEndDate");
            ReplaceColumn(table, "PurchaseDate", "OrderDate");
            ReplaceColumn(table, "PartnerTotal", "Total");

            DropRows(table, r => Safe(r, "ProductName") == "Azure plan");

            // Make sure every Microsoft column exists, even if blank, then reorder
            foreach (var col in MicrosoftColumns.Where(c => !table.Columns.Contains(c)))
                table.Columns.Add(col, typeof(string));

            return ReorderColumns(table, UniqueKeyColumns);
        }

        // ─────────────────────────────────────────────────────────────
        // 5.  HELPERS
        // ─────────────────────────────────────────────────────────────

        private static void DropRows(DataTable table, Func<DataRow, bool> predicate)
        {
            for (int i = table.Rows.Count - 1; i >= 0; i--)
                if (predicate(table.Rows[i])) table.Rows.RemoveAt(i);
        }

        private static void ReplaceColumn(DataTable table, string oldName, string newName)
        {
            if (!table.Columns.Contains(oldName)) return;
            if (table.Columns.Contains(newName)) table.Columns.Remove(newName);
            table.Columns[oldName].ColumnName = newName;
        }

        private static DataTable ReorderColumns(DataTable table, string[] firstColumns)
        {
            var newTable = new DataTable();

            // 1️⃣  Desired columns first
            foreach (var col in firstColumns.Where(table.Columns.Contains))
                newTable.Columns.Add(col, table.Columns[col].DataType);

            // 2️⃣  Everything else afterwards (preserve original order)
            foreach (DataColumn c in table.Columns)
                if (!newTable.Columns.Contains(c.ColumnName))
                    newTable.Columns.Add(c.ColumnName, c.DataType);

            // Copy data row‑by‑row
            foreach (DataRow row in table.Rows)
            {
                var newRow = newTable.NewRow();
                foreach (DataColumn col in newTable.Columns)
                    newRow[col.ColumnName] = row[col.ColumnName];
                newTable.Rows.Add(newRow);
            }

            return newTable;
        }

        private static void SplitTermAndCycle(
            DataTable table,
            string sourceColumn = "TermAndBillingCycle",
            string termColumn = "Term",
            string cycleColumn = "BillingCycle")
        {
            if (!table.Columns.Contains(sourceColumn)) return;

            table.Columns.Add(termColumn, typeof(string));
            table.Columns.Add(cycleColumn, typeof(string));

            foreach (DataRow row in table.Rows)
            {
                var composite = Safe(row, sourceColumn);
                var billing = Safe(row, "BillingFrequency");

                DetermineTermAndBillingCycle(composite, billing,
                    out string term, out string cycle);

                row[termColumn] = term;
                row[cycleColumn] = cycle;
            }
        }

        private static void DetermineTermAndBillingCycle(
            string composite, string billing,
            out string term, out string cycle)
        {
            term = "NA";
            cycle = "NA";

            switch (composite)
            {
                case "One-Year commitment for monthly/yearly billing":
                    term = "Annual";
                    cycle = string.IsNullOrEmpty(billing) ? "Annual" : "Monthly";
                    break;

                case "One-Year commitment for yearly billing":
                    term = "Annual";
                    cycle = "Annual";
                    break;

                case "One-Month commitment for monthly billing":
                case "1 Cache Instance Hour":
                    term = "Monthly";
                    cycle = "Monthly";
                    break;

                case "Three-3 Years commitment for monthly/3 Years/yearly billing":
                    term = "Triennial";
                    cycle = "Monthly";
                    break;

                case "":
                case null:
                    term = "None";
                    cycle = "OneTime";
                    break;
            }
        }

        private static string Safe(DataRow row, string col) =>
            row.Table.Columns.Contains(col) && row[col] != DBNull.Value
                ? row[col]?.ToString() ?? string.Empty
                : string.Empty;
    }
}
