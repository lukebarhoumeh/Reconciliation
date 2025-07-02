using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace Reconciliation
{
    /// <summary>
    /// Detects price or quantity mismatches between MSP‑Hub and Microsoft invoices
    /// and exports them to Excel.
    /// </summary>
    public class PriceMismatchService
    {
        // ------------------------------------------------------------------
        //  Configuration
        // ------------------------------------------------------------------
        private static readonly string[] KeyColumns =
        {
            "CustomerDomainName",
            "ProductId"
        };

        private const string AzurePlan = "Azure plan";

        // ------------------------------------------------------------------
        //  Public API
        // ------------------------------------------------------------------
        public DataTable GetPriceMismatches(DataTable msphub, DataTable microsoft)
        {
            if (msphub == null) throw new ArgumentNullException(nameof(msphub));
            if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));

            // 1. Remove Azure‑plan usage rows (not licence seats)
            var hubFiltered = msphub.AsEnumerable()
                                    .Where(r => !IsAzurePlan(r, "ProductName"))
                                    .ToArray();

            var msFiltered = microsoft.AsEnumerable()
                                       .Where(r => !IsAzurePlan(r, "SubscriptionDescription"))
                                       .ToArray();

            if (hubFiltered.Length == 0 || msFiltered.Length == 0)
                return msphub.Clone();   // nothing left to compare

            // 2. Group by deterministic key
            var hubGroups = hubFiltered.GroupBy(MakeKey);
            var msGroups = msFiltered.GroupBy(MakeKey)
                                      .ToDictionary(g => g.Key, g => g);

            // 3. Result table
            var result = msphub.Clone();
            result.Columns.Add("HubQuantity", typeof(decimal));
            result.Columns.Add("MSQuantity", typeof(decimal));
            result.Columns.Add("HubSubtotal", typeof(decimal));
            result.Columns.Add("MSSubtotal", typeof(decimal));
            result.Columns.Add("QuantityDiff", typeof(decimal));
            result.Columns.Add("PriceDiff", typeof(decimal));

            // 4. Comparison loop
            foreach (var hGroup in hubGroups)
            {
                if (!msGroups.TryGetValue(hGroup.Key, out var mGroup))
                    continue;       // appears only in MSP‑Hub: handled elsewhere

                decimal hubQty = hGroup.Sum(r => SafeDecimal(r["Quantity"]));
                decimal hubSub = hGroup.Sum(r => SafeDecimal(r["Subtotal"]));
                decimal msQty = mGroup.Sum(r => SafeDecimal(r["Quantity"]));
                decimal msSub = mGroup.Sum(r => SafeDecimal(r["Subtotal"]));

                decimal qtyDiff = hubQty - msQty;
                decimal subDiff = hubSub - msSub;

                if (Math.Abs(qtyDiff) <= AppConfig.Validation.NumericTolerance &&
                    Math.Abs(subDiff) <= AppConfig.Validation.NumericTolerance)
                    continue;       // no material difference

                // 5. Write one representative row
                var row = result.NewRow();
                var parts = hGroup.Key.Split('|');
                for (int i = 0; i < KeyColumns.Length; i++)
                    row[KeyColumns[i]] = parts[i];

                if (result.Columns.Contains("ChargeType"))
                {
                    row["ChargeType"] = string.Join(", ",
                        hGroup.Where(r => r.Table.Columns.Contains("ChargeType"))
                               .Select(r => r["ChargeType"]).Distinct());
                }
                row["HubQuantity"] = hubQty;
                row["MSQuantity"] = msQty;
                row["HubSubtotal"] = hubSub;
                row["MSSubtotal"] = msSub;
                row["QuantityDiff"] = qtyDiff;
                row["PriceDiff"] = subDiff;

                result.Rows.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Simple Excel export with header styling and auto‑fit.
        /// </summary>
        public void ExportPriceMismatchesToExcel(DataTable mismatches, string filePath)
        {
            if (mismatches == null) throw new ArgumentNullException(nameof(mismatches));
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Mismatches");

            // header
            for (int c = 0; c < mismatches.Columns.Count; c++)
            {
                var cell = ws.Cells[1, c + 1];
                cell.Value = mismatches.Columns[c].ColumnName;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#D9EAF7"));
                cell.Style.Font.Bold = true;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
            }

            // rows
            for (int r = 0; r < mismatches.Rows.Count; r++)
            {
                for (int c = 0; c < mismatches.Columns.Count; c++)
                {
                    var cell = ws.Cells[r + 2, c + 1];
                    cell.Value = mismatches.Rows[r][c];
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
                }
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            package.SaveAs(new System.IO.FileInfo(filePath));
        }

        // ------------------------------------------------------------------
        //  Helpers
        // ------------------------------------------------------------------
        private static bool IsAzurePlan(DataRow r, string column) =>
            r.Table.Columns.Contains(column) &&
            string.Equals(Convert.ToString(r[column]), AzurePlan,
                          StringComparison.OrdinalIgnoreCase);

        private static string MakeKey(DataRow r)
        {
            string customer = r.Table.Columns.Contains("CustomerDomainName")
                ? Convert.ToString(r["CustomerDomainName"]) ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(customer) && r.Table.Columns.Contains("CustomerName"))
                customer = Convert.ToString(r["CustomerName"]) ?? string.Empty;

            string product = r.Table.Columns.Contains("ProductId")
                ? Convert.ToString(r["ProductId"]) ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(product) && r.Table.Columns.Contains("PartNumber"))
                product = Convert.ToString(r["PartNumber"]) ?? string.Empty;

            return string.Join("|", customer.Trim().ToUpperInvariant(), product.Trim().ToUpperInvariant());
        }

        private static decimal SafeDecimal(object? v) =>
            decimal.TryParse(Convert.ToString(v), NumberStyles.Any,
                             CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }
}
