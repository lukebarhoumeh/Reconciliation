using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Reconciliation
{
    public static class CsvNormalizer
    {
        // ------------------------------------------------------------------
        //  Metadata
        // ------------------------------------------------------------------
        private static readonly HashSet<string> NumericColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "UnitPrice", "EffectiveUnitPrice", "Quantity",
            "Subtotal",  "TaxTotal",          "Total",
            "PriceAdjustment"
        };

        private static readonly string[] DateColumnSuffixes =
        {
            "Date",      // ChargeStartDate, OrderDate, etc.
            "DateUtc",
            "Time"       // rare but covered just in case
        };

        private const string CanonicalDateFormat = "yyyy-MM-dd";

        private static readonly string[] AcceptedDateFormats =
        {
            // ISO‑8601
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fff",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd",
            // U.S. Excel‑style
            "MM/dd/yyyy HH:mm:ss",
            "M/d/yyyy HH:mm:ss",
            "MM/dd/yyyy",
            "M/d/yyyy"
        };

        // ------------------------------------------------------------------
        //  Public API
        // ------------------------------------------------------------------
        public static DataView NormalizeCsv(string filePath)
        {
            using var parser = new TextFieldParser(filePath)
            {
                Delimiters = new[] { "," },
                HasFieldsEnclosedInQuotes = true
            };

            var dt = new DataTable();

            // --- headers ---------------------------------------------------
            if (!parser.EndOfData)
            {
                foreach (var header in parser.ReadFields() ?? Array.Empty<string>())
                    dt.Columns.Add(CleanString(header));
            }

            // --- rows ------------------------------------------------------
            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields() ?? Array.Empty<string>();
                var row = dt.NewRow();

                for (int i = 0; i < dt.Columns.Count; i++)
                    row[i] = i < fields.Length ? fields[i] : string.Empty;

                dt.Rows.Add(row);
            }

            NormalizeRows(dt, Path.GetFileName(filePath));
            return dt.DefaultView;
        }

        // Overload used in tests
        public static DataView NormalizeDataTable(DataTable table)
        {
            NormalizeRows(table, string.Empty);
            return table.DefaultView;
        }

        // ------------------------------------------------------------------
        //  Core row normalisation
        // ------------------------------------------------------------------
        private static void NormalizeRows(DataTable table, string fileName)
        {
            int line = 1;   // 1‑based for user‑friendly error logs

            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn column in table.Columns)
                {
                    string raw = row[column]?.ToString() ?? string.Empty;
                    string cleaned = CleanString(raw);

                    if (IsDateColumn(column.ColumnName))
                    {
                        row[column] = NormalizeDate(cleaned, fileName, line, column.ColumnName);
                    }
                    else if (IsNumericColumn(column.ColumnName))
                    {
                        row[column] = NormalizeNumber(cleaned, fileName, line, column.ColumnName);
                    }
                    else
                    {
                        row[column] = NormalizeText(cleaned, column.ColumnName);
                    }
                }
                line++;
            }
        }

        // ------------------------------------------------------------------
        //  Normalisation helpers
        // ------------------------------------------------------------------
        private static string NormalizeDate(string input, string file, int line, string col)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Try exact formats first (fast)
            if (DateTime.TryParseExact(input.Trim(), AcceptedDateFormats,
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                       out var dt))
            {
                return dt.ToString(CanonicalDateFormat, CultureInfo.InvariantCulture);
            }

            // Fallback: broad TryParse (catches locale‑specific Excel exports)
            if (DateTime.TryParse(input, CultureInfo.InvariantCulture,
                                  DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                  out dt))
            {
                return dt.ToString(CanonicalDateFormat, CultureInfo.InvariantCulture);
            }

            // Could not parse – log & return original string so it will still be visible
            ErrorLogger.LogError(
                line,
                col,
                $"Invalid date '{input}' – unable to parse.",
                input,
                file,
                string.Empty);

            return input.Trim();
        }

        private static string NormalizeNumber(string input, string file, int line, string col)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string digitsOnly = Regex.Replace(input, @"[^\d\.\-]", string.Empty);

            if (decimal.TryParse(digitsOnly, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                return num.ToString(CultureInfo.InvariantCulture);

            ErrorLogger.LogError(
                line,
                col,
                $"Invalid numeric '{input}' – unable to parse.",
                input,
                file,
                string.Empty);

            return input.Trim();
        }

        private static string NormalizeText(string input, string colName)
        {
            // Collapse multiple whitespace to a single space
            string txt = Regex.Replace(input, @"\s+", " ");

            // Optionally drop leading zeros (but preserve them for IDs/SKUs)
            if (ShouldRemoveLeadingZeros(colName))
                txt = txt.TrimStart('0');

            return txt;
        }

        // ------------------------------------------------------------------
        //  Column‑type helpers
        // ------------------------------------------------------------------
        private static bool IsNumericColumn(string name) =>
            NumericColumns.Contains(name);

        private static bool IsDateColumn(string name)
        {
            name ??= string.Empty;
            foreach (var suffix in DateColumnSuffixes)
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        private static bool ShouldRemoveLeadingZeros(string name)
        {
            name = name?.ToLowerInvariant() ?? string.Empty;
            return !(name.Contains("id") || name.Contains("sku"));
        }

        // ------------------------------------------------------------------
        //  Generic string cleaning (control chars, trimming)
        // ------------------------------------------------------------------
        private static string CleanString(string input)
        {
            if (input == null) return string.Empty;

            // Remove non‑printing control characters (keep CR/LF/TAB)
            string noControl = Regex.Replace(input, @"[\p{C}&&[^\r\n\t]]", string.Empty);
            return noControl.Trim();
        }
    }
}
