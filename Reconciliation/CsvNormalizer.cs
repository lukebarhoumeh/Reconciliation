using Microsoft.VisualBasic.FileIO;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Reconciliation
{
    public static class CsvNormalizer
    {
        public static DataView NormalizeCsv(string filePath)
        {
            using var parser = new TextFieldParser(filePath);
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;
            DataTable dt = new DataTable();

            if (!parser.EndOfData)
            {
                var headers = parser.ReadFields() ?? Array.Empty<string>();
                foreach (var h in headers)
                {
                    dt.Columns.Add(CleanString(h));
                }
            }

            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields() ?? Array.Empty<string>();
                var row = dt.NewRow();
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    row[i] = i < fields.Length ? fields[i] : string.Empty;
                }
                dt.Rows.Add(row);
            }

            NormalizeRows(dt);
            return dt.DefaultView;
        }

        public static DataView NormalizeDataTable(DataTable table)
        {
            NormalizeRows(table);
            return table.DefaultView;
        }

        private static void NormalizeRows(DataTable table)
        {
            int line = 1;
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    var column = table.Columns[i];
                    string value = row[column]?.ToString() ?? string.Empty;
                    string cleaned = CleanString(value);

                    if (IsDateColumn(column.ColumnName))
                    {
                        if (string.IsNullOrWhiteSpace(cleaned))
                        {
                            row[column] = string.Empty;
                        }
                        else if (DateTime.TryParse(cleaned, out DateTime d))
                        {
                            row[column] = d.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            ErrorLogger.LogError($"Row {line}: The column '{column.ColumnName}' expected a date but found '{cleaned}'");
                            row[column] = cleaned;
                        }
                    }
                    else if (IsNumericColumn(column.ColumnName))
                    {
                        string digits = Regex.Replace(cleaned, "[^0-9.-]", string.Empty);
                        if (string.IsNullOrWhiteSpace(cleaned))
                        {
                            row[column] = string.Empty;
                        }
                        else if (decimal.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal num))
                        {
                            row[column] = num.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            ErrorLogger.LogError($"Row {line}: The column '{column.ColumnName}' expected a numeric value but found '{cleaned}'");
                            row[column] = cleaned;
                        }
                    }
                    else
                    {
                        string normalized = Regex.Replace(cleaned, "\\s+", " ");
                        if (ShouldRemoveLeadingZeros(column.ColumnName))
                            normalized = normalized.TrimStart('0');
                        row[column] = normalized;
                    }
                }
                line++;
            }
        }

        private static bool IsNumericColumn(string name)
        {
            name = name.ToLowerInvariant();
            string[] hints = { "price", "amount", "quantity", "total", "unit", "rate" };
            return hints.Any(h => name.Contains(h));
        }

        private static bool IsDateColumn(string name)
        {
            return name.ToLowerInvariant().Contains("date");
        }

        private static bool ShouldRemoveLeadingZeros(string name)
        {
            name = name.ToLowerInvariant();
            return !(name.Contains("id") || name.Contains("sku"));
        }

        private static string CleanString(string input)
        {
            if (input == null) return string.Empty;
            var withoutControl = Regex.Replace(input, "[\\p{C}]", string.Empty);
            return withoutControl.Trim();
        }
    }
}
