using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Reconciliation
{
    public static class DataQualityValidator
    {
        public static void Run(DataTable table, string fileName)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));

            int rowIndex = 1;
            foreach (DataRow row in table.Rows)
            {
                decimal quantity = ParseDecimal(row, "Quantity");
                bool qtyPositive = quantity > 0;
                string ctx = GetContext(row);

                string[] fields =
                {
                    "PartnerDiscountPercentage",
                    "PartnerEffectiveUnitPrice",
                    "CustomerUnitPrice",
                    "CustomerPerDayUnitPrice",
                    "CustomerEffectiveUnitPrice",
                    "PartnerTaxTotal"
                };

                foreach (var col in fields)
                {
                    if (!table.Columns.Contains(col))
                        continue;
                    string raw = Convert.ToString(row[col]) ?? string.Empty;
                    decimal val = ParseDecimal(raw);
                    if (qtyPositive && (string.IsNullOrWhiteSpace(raw) || val == 0))
                    {
                        ErrorLogger.LogWarning(rowIndex, col,
                            $"Value is 0 or blank while Quantity is {quantity}", raw, fileName, ctx);
                    }

                    if (col.Contains("Discount", StringComparison.OrdinalIgnoreCase))
                    {
                        if (val < 0 || val > 100)
                            ErrorLogger.LogError(rowIndex, col,
                                "Discount percentage out of bounds (0-100)", raw, fileName, ctx);
                        else if (val < 1 || val > 90)
                            ErrorLogger.LogWarning(rowIndex, col,
                                "Suspicious discount (<1% or >90%)", raw, fileName, ctx);
                    }
                }

                bool allZero = fields.All(f =>
                    !table.Columns.Contains(f) || ParseDecimal(row, f) == 0);
                if (qtyPositive && allZero)
                {
                    ErrorLogger.LogError(rowIndex, "-",
                        "All pricing fields zero or blank while Quantity not zero",
                        string.Empty, fileName, ctx);
                }

                foreach (DataColumn c in table.Columns)
                {
                    string name = c.ColumnName.ToLowerInvariant();
                    string raw = Convert.ToString(row[c]) ?? string.Empty;

                    if (name.Contains("date") &&
                        !string.IsNullOrWhiteSpace(raw) &&
                        !DateTime.TryParse(raw, out _))
                    {
                        ErrorLogger.LogError(rowIndex, c.ColumnName,
                            "Invalid date format", raw, fileName, ctx);
                    }

                    if (name.Contains("total"))
                    {
                        decimal val = ParseDecimal(raw);
                        if (val < 0)
                        {
                            ErrorLogger.LogWarning(rowIndex, c.ColumnName,
                                "Negative total value", val.ToString(CultureInfo.InvariantCulture),
                                fileName, ctx);
                        }
                    }
                }

                rowIndex++;
            }
        }

        private static decimal ParseDecimal(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column))
                return 0m;
            return ParseDecimal(Convert.ToString(row[column]));
        }

        private static decimal ParseDecimal(string? input)
        {
            return decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? d
                : 0m;
        }

        private static string GetContext(DataRow row)
        {
            string customer = GetValue(row, "CustomerName");
            string sku = GetValue(row, "SkuId");
            return $"Customer: {customer}, SKU: {sku}";
        }

        private static string GetValue(DataRow row, string column)
        {
            return row.Table.Columns.Contains(column)
                ? Convert.ToString(row[column]) ?? string.Empty
                : string.Empty;
        }
    }
}
