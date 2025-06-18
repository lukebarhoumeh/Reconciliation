using System;
using System.Data;
using System.Linq;
using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Reconciliation
{
    /// <summary>
    /// Provides price mismatch detection and Excel export functionality.
    /// </summary>
    public class PriceMismatchService
    {
        private readonly string[] _keyColumns = new[]
        {
            "CustomerDomainName",
            "ProductId",
            "SkuId",
            "ChargeType",
            "Term",
            "BillingCycle"
        };

        /// <summary>
        /// Returns rows where total price differs between MSP Hub and Microsoft invoices.
        /// </summary>
        public DataTable GetPriceMismatches(DataTable msphub, DataTable microsoft)
        {
            if (msphub == null) throw new ArgumentNullException(nameof(msphub));
            if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));

            var filteredHub = msphub.AsEnumerable()
                .Where(r => !string.Equals(r["ProductName"].ToString(), "Azure plan", StringComparison.OrdinalIgnoreCase))
                .CopyToDataTable();
            var filteredMs = microsoft.AsEnumerable()
                .Where(r => !string.Equals(r["SubscriptionDescription"].ToString(), "Azure plan", StringComparison.OrdinalIgnoreCase))
                .CopyToDataTable();

            DataTable result = msphub.Clone();
            result.Columns.Add("PriceInSixDotOne", typeof(decimal));
            result.Columns.Add("PriceInMicrosoft", typeof(decimal));
            result.Columns.Add("PriceDifference", typeof(decimal));

            var groupedHub = filteredHub.AsEnumerable()
                .GroupBy(row => string.Join("|", _keyColumns.Select(c => row[c])));
            var groupedMs = filteredMs.AsEnumerable()
                .GroupBy(row => string.Join("|", _keyColumns.Select(c => row[c])));

            foreach (var hubGroup in groupedHub)
            {
                var msGroup = groupedMs.FirstOrDefault(g => g.Key == hubGroup.Key);
                if (msGroup == null) continue;

                decimal qtyHub = hubGroup.Sum(r => SafeDecimal(r["Quantity"]));
                decimal priceHub = hubGroup.Sum(r => SafeDecimal(r["EffectiveUnitPrice"]) * SafeDecimal(r["Quantity"]));
                decimal priceMs = msGroup.Sum(r => SafeDecimal(r["EffectiveUnitPrice"]) * SafeDecimal(r["Quantity"]));
                decimal diff = priceHub - priceMs;
                if (diff == 0) continue;

                DataRow row = result.NewRow();
                var keys = hubGroup.Key.Split('|');
                for (int i = 0; i < _keyColumns.Length; i++)
                    row[_keyColumns[i]] = keys[i];
                foreach (DataColumn col in filteredHub.Columns)
                {
                    if (_keyColumns.Contains(col.ColumnName) || col.ColumnName == "Quantity")
                        continue;
                    row[col.ColumnName] = hubGroup.First()[col];
                }
                row["Quantity"] = qtyHub;
                row["PriceInSixDotOne"] = priceHub;
                row["PriceInMicrosoft"] = priceMs;
                row["PriceDifference"] = diff;
                result.Rows.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Exports mismatched rows to an Excel file with colored headers.
        /// </summary>
        public void ExportPriceMismatchesToExcel(DataTable mismatches, string filePath)
        {
            if (mismatches == null) throw new ArgumentNullException(nameof(mismatches));
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            using var package = new ExcelPackage();
            ExcelWorksheet ws = package.Workbook.Worksheets.Add("Sheet1");
            AddHeaderRow(mismatches, ws);
            AddDataRowsWithColor(mismatches, ws);
            package.SaveAs(new System.IO.FileInfo(filePath));
        }

        private static decimal SafeDecimal(object? value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            return Convert.ToDecimal(value);
        }

        private static void AddHeaderRow(DataTable table, ExcelWorksheet ws)
        {
            for (int col = 0; col < table.Columns.Count; col++)
            {
                var header = ws.Cells[1, col + 1];
                header.Value = table.Columns[col].ColumnName;
                header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                header.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#D9EAF7"));
                header.Style.Font.Bold = true;
                header.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                header.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                header.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                header.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                header.Style.Border.Top.Color.SetColor(Color.Black);
                header.Style.Border.Bottom.Color.SetColor(Color.Black);
                header.Style.Border.Left.Color.SetColor(Color.Black);
                header.Style.Border.Right.Color.SetColor(Color.Black);
            }
        }

        private static void AddDataRowsWithColor(DataTable table, ExcelWorksheet ws)
        {
            for (int r = 0; r < table.Rows.Count; r++)
            {
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    var cell = ws.Cells[r + 2, c + 1];
                    cell.Value = table.Rows[r][c];
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Top.Color.SetColor(Color.Black);
                    cell.Style.Border.Bottom.Color.SetColor(Color.Black);
                    cell.Style.Border.Left.Color.SetColor(Color.Black);
                    cell.Style.Border.Right.Color.SetColor(Color.Black);
                }
            }
        }
    }
}
