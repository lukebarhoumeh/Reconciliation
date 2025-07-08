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
            "CustomerId",
            "ProductId",
            "ChargeType",
            "SubscriptionId"
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
                return BuildResultTable();   // nothing left to compare

            // 2. Group by deterministic key
            var hubGroups = hubFiltered.GroupBy(MakeKey);
            var msGroups = msFiltered.GroupBy(MakeKey)
                                      .ToDictionary(g => g.Key, g => g);

            // 3. Result table
            var result = BuildResultTable();

            // 4. Comparison loop
            foreach (var hGroup in hubGroups)
            {
                if (!msGroups.TryGetValue(hGroup.Key, out var mGroup))
                    continue;       // appears only in MSP‑Hub: handled elsewhere

                decimal hubQty = hGroup.Sum(r => SafeDecimal(r["Quantity"]));
                decimal hubSub = hGroup.Sum(r => SafeDecimal(r["Subtotal"]));
                decimal hubTotal = hGroup.Sum(r => SafeDecimal(r.Table.Columns.Contains("Total") ? r["Total"] : null));
                decimal hubTax = hGroup.Sum(r => SafeDecimal(r.Table.Columns.Contains("TaxTotal") ? r["TaxTotal"] : null));

                decimal msQty = mGroup.Sum(r => SafeDecimal(r["Quantity"]));
                decimal msSub = mGroup.Sum(r => SafeDecimal(r["Subtotal"]));
                decimal msTotal = mGroup.Sum(r => SafeDecimal(r.Table.Columns.Contains("Total") ? r["Total"] : null));
                decimal msTax = mGroup.Sum(r => SafeDecimal(r.Table.Columns.Contains("TaxTotal") ? r["TaxTotal"] : null));

                decimal qtyDiff = hubQty - msQty;
                decimal subDiff = hubSub - msSub;

                if (Math.Abs(qtyDiff) <= AppConfig.Validation.NumericTolerance &&
                    Math.Abs(subDiff) <= AppConfig.Validation.NumericTolerance)
                    continue;       // no material difference

                // 5. Write one representative row
                var row = result.NewRow();
                var parts = hGroup.Key.Split('|');
                if (result.Columns.Contains("CustomerId"))
                    row["CustomerId"] = parts.ElementAtOrDefault(0);
                if (result.Columns.Contains("ProductId"))
                    row["ProductId"] = parts.ElementAtOrDefault(1);
                if (result.Columns.Contains("ChargeType"))
                    row["ChargeType"] = parts.ElementAtOrDefault(2);
                if (result.Columns.Contains("SubscriptionId"))
                    row["SubscriptionId"] = parts.ElementAtOrDefault(3);
                if (result.Columns.Contains("Status"))
                    row["Status"] = "Mismatched";
                if (result.Columns.Contains("HubQuantity"))
                    row["HubQuantity"] = hubQty;
                if (result.Columns.Contains("MSQuantity"))
                    row["MSQuantity"] = msQty;
                if (result.Columns.Contains("HubSubtotal"))
                    row["HubSubtotal"] = hubSub;
                if (result.Columns.Contains("MSSubtotal"))
                    row["MSSubtotal"] = msSub;
                if (result.Columns.Contains("HubTotal"))
                    row["HubTotal"] = hubTotal;
                if (result.Columns.Contains("MSTotal"))
                    row["MSTotal"] = msTotal;
                if (result.Columns.Contains("HubTaxTotal"))
                    row["HubTaxTotal"] = hubTax;
                if (result.Columns.Contains("MSTaxTotal"))
                    row["MSTaxTotal"] = msTax;
                if (result.Columns.Contains("PriceDiff"))
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

        private static DataTable BuildResultTable()
        {
            var t = new DataTable();
            t.Columns.Add("CustomerId");
            t.Columns.Add("ProductId");
            t.Columns.Add("ChargeType");
            t.Columns.Add("SubscriptionId");
            t.Columns.Add("Status");
            t.Columns.Add("HubQuantity", typeof(decimal));
            t.Columns.Add("MSQuantity", typeof(decimal));
            t.Columns.Add("HubSubtotal", typeof(decimal));
            t.Columns.Add("MSSubtotal", typeof(decimal));
            t.Columns.Add("HubTotal", typeof(decimal));
            t.Columns.Add("MSTotal", typeof(decimal));
            t.Columns.Add("HubTaxTotal", typeof(decimal));
            t.Columns.Add("MSTaxTotal", typeof(decimal));
            t.Columns.Add("PriceDiff", typeof(decimal));
            return t;
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
            string customer = string.Empty;
            if (r.Table.Columns.Contains("CustomerId"))
                customer = Convert.ToString(r["CustomerId"]) ?? string.Empty;
            else if (r.Table.Columns.Contains("CustomerDomainName"))
                customer = Convert.ToString(r["CustomerDomainName"]) ?? string.Empty;
            else if (r.Table.Columns.Contains("CustomerName"))
                customer = Convert.ToString(r["CustomerName"]) ?? string.Empty;

            string product = string.Empty;
            if (r.Table.Columns.Contains("ProductId"))
                product = Convert.ToString(r["ProductId"]) ?? string.Empty;
            else if (r.Table.Columns.Contains("PartNumber"))
                product = Convert.ToString(r["PartNumber"]) ?? string.Empty;

            string charge = r.Table.Columns.Contains("ChargeType")
                ? Convert.ToString(r["ChargeType"]) ?? string.Empty
                : string.Empty;

            string sub = string.Empty;
            if (r.Table.Columns.Contains("SubscriptionId"))
                sub = Convert.ToString(r["SubscriptionId"]) ?? string.Empty;
            else if (r.Table.Columns.Contains("SkuId"))
                sub = Convert.ToString(r["SkuId"]) ?? string.Empty;

            return string.Join("|",
                (customer?.Trim().ToUpperInvariant() ?? string.Empty),
                (product?.Trim().ToUpperInvariant() ?? string.Empty),
                (charge?.Trim().ToUpperInvariant() ?? string.Empty),
                (sub?.Trim().ToUpperInvariant() ?? string.Empty));
        }

        private static decimal SafeDecimal(object? v) =>
            decimal.TryParse(Convert.ToString(v), NumberStyles.Any,
                             CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }
}
