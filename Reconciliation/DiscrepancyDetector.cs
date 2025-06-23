using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Reconciliation
{
    /// <summary>
    /// Detects discrepancies between two <see cref="DataTable"/> instances with
    /// support for numeric tolerances, date tolerances and fuzzy text matching.
    /// </summary>
    public class DiscrepancyDetector
    {
        /// <summary>Maximum numeric difference considered equal.</summary>
        public decimal NumericTolerance { get; set; } = AppConfig.Validation.NumericTolerance;

        /// <summary>Maximum allowed days between two dates to be considered equal.</summary>
        public TimeSpan DateTolerance { get; set; } = TimeSpan.FromDays(AppConfig.Validation.DateToleranceDays);


        private readonly List<Discrepancy> _discrepancies = new();
        private readonly Dictionary<string, int> _summary = new();
        private int _rowsCompared;

        /// <summary>List of found discrepancies.</summary>
        public IReadOnlyList<Discrepancy> Discrepancies => _discrepancies.AsReadOnly();

        /// <summary>Grouped summary of discrepancy explanations.</summary>
        public IReadOnlyDictionary<string, int> Summary => _summary;

        /// <summary>
        /// Compare two tables row by row and record discrepancies.
        /// </summary>
        public void Compare(DataTable left, DataTable right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            _discrepancies.Clear();
            _summary.Clear();
            _rowsCompared = Math.Max(left.Rows.Count, right.Rows.Count);

            for (int i = 0; i < _rowsCompared; i++)
            {
                bool hasLeft = i < left.Rows.Count;
                bool hasRight = i < right.Rows.Count;
                if (!hasLeft || !hasRight)
                {
                    string reason = "Missing Rows";
                    AddSummary(reason);
                    if (!hasLeft)
                        _discrepancies.Add(new Discrepancy(i + 1, string.Empty, string.Empty, string.Empty, "Row missing in left table"));
                    if (!hasRight)
                        _discrepancies.Add(new Discrepancy(i + 1, string.Empty, string.Empty, string.Empty, "Row missing in right table"));
                    continue;
                }

                foreach (DataColumn col in left.Columns)
                {
                    if (!right.Columns.Contains(col.ColumnName))
                        continue;

                    string a = Convert.ToString(left.Rows[i][col]) ?? string.Empty;
                    string b = Convert.ToString(right.Rows[i][col.ColumnName]) ?? string.Empty;
                    if (IsEqual(a, b, col.ColumnName))
                        continue;
                    string expl = ExplainDifference(a, b, col.ColumnName);
                    string group = GetGroupName(col.ColumnName, expl);
                    AddSummary(group);
                    _discrepancies.Add(new Discrepancy(i + 1, col.ColumnName, a, b, expl));
                }
            }
        }

        private bool IsEqual(string a, string b, string column)
        {
            a = a.Trim();
            b = b.Trim();
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
                return true;

            if (decimal.TryParse(a, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal da) &&
                decimal.TryParse(b, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal db))
            {
                return Math.Abs(da - db) <= NumericTolerance;
            }
            if (DateTime.TryParse(a, out DateTime ta) && DateTime.TryParse(b, out DateTime tb))
            {
                return Math.Abs((ta - tb).TotalDays) <= DateTolerance.TotalDays;
            }
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private string ExplainDifference(string a, string b, string column)
        {
            if (decimal.TryParse(a, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal da) &&
                decimal.TryParse(b, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal db))
            {
                bool percent = column.Contains("Percent", StringComparison.OrdinalIgnoreCase);
                string left = percent ? NumericFormatter.Percent(da) : NumericFormatter.Money(da);
                string right = percent ? NumericFormatter.Percent(db) : NumericFormatter.Money(db);
                string friendly = FriendlyNameMap.Get(column);
                return $"Numeric mismatch in {friendly}: {left} vs {right}";
            }
            if (DateTime.TryParse(a, out DateTime ta) && DateTime.TryParse(b, out DateTime tb))
            {
                string friendly = FriendlyNameMap.Get(column);
                return $"Date mismatch in {friendly}: {ta:d} vs {tb:d}";
            }
            return $"Text mismatch in {FriendlyNameMap.Get(column)}: '{a}' vs '{b}'";
        }

        private static string GetGroupName(string column, string explanation)
        {
            if (explanation.StartsWith("Row missing"))
                return "Missing Rows";
            if (column.Equals("SkuId", StringComparison.OrdinalIgnoreCase))
                return "Product Code mismatches";
            if (column.Equals("Quantity", StringComparison.OrdinalIgnoreCase))
                return "Quantity mismatches";
            if (column.Contains("Date", StringComparison.OrdinalIgnoreCase))
                return "Date mismatches";
            return "Other mismatches";
        }

        private void AddSummary(string group)
        {
            if (_summary.ContainsKey(group))
                _summary[group]++;
            else
                _summary[group] = 1;
        }

        private static string FormatValue(string value, string column)
        {
            if (decimal.TryParse(value.TrimEnd('%'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                bool percent = column.Contains("Percent", StringComparison.OrdinalIgnoreCase) || value.Trim().EndsWith("%");
                string formatted = percent ? NumericFormatter.Percent(d) : NumericFormatter.Money(d);
                return percent && value.Trim().EndsWith("%") ? formatted + "%" : formatted;
            }
            return value;
        }

        /// <summary>
        /// Convert collected discrepancies into a <see cref="DataTable"/>.
        /// </summary>
        public DataTable GetMismatches()
        {
            var table = new DataTable();
            table.Columns.Add("Row Number", typeof(int));
            table.Columns.Add("Field Name", typeof(string));
            table.Columns.Add("Our Value", typeof(string));
            table.Columns.Add("Microsoft Value", typeof(string));
            table.Columns.Add("Explanation", typeof(string));
            table.Columns.Add("Suggested Action", typeof(string));

            foreach (var d in _discrepancies)
            {
                var row = table.NewRow();
                string group = GetGroupName(d.Column, d.Explanation);
                row["Row Number"] = d.Row;
                row["Field Name"] = FriendlyNameMap.Get(d.Column);
                row["Our Value"] = FormatValue(d.LeftValue, d.Column);
                row["Microsoft Value"] = FormatValue(d.RightValue, d.Column);
                row["Explanation"] = d.Explanation;
                row["Suggested Action"] = GetSuggestedAction(group);
                table.Rows.Add(row);
            }

            return table;
        }

        /// <summary>
        /// Generate a human readable summary of the comparison.
        /// </summary>
        public string GetSummary()
        {
            var sb = new StringBuilder();
            string summaryLine = string.Join(", ", _summary.Select(kv => $"{kv.Value} {kv.Key}"));
            sb.AppendLine($"Summary: {summaryLine}");
            sb.AppendLine($"Total rows compared: {_rowsCompared}");
            sb.AppendLine($"Discrepancies found: {_discrepancies.Count}");
            foreach (var kv in _summary)
                sb.AppendLine($"{kv.Value} rows in group: {kv.Key}");
            return sb.ToString();
        }

        /// <summary>
        /// Export discrepancies to a CSV file.
        /// </summary>
        public void ExportCsv(string path)
        {
            string summaryLine = string.Join(", ", _summary.Select(kv => $"{kv.Value} {kv.Key}"));
            var lines = new List<string>
            {
                $"Summary: {summaryLine}",
                "Row Number,Field Name,Our Value,Microsoft Value,Explanation,Suggested Action"
            };
            foreach (var d in _discrepancies)
            {
                string left = FormatValue(d.LeftValue, d.Column);
                string right = FormatValue(d.RightValue, d.Column);
                string group = GetGroupName(d.Column, d.Explanation);
                string action = GetSuggestedAction(group);
                string line = string.Join(',',
                    d.Row,
                    Escape(FriendlyNameMap.Get(d.Column)),
                    Escape(left),
                    Escape(right),
                    Escape(d.Explanation),
                    Escape(action));
                lines.Add(line);
            }
            File.WriteAllLines(path, lines);
        }

        private static string Escape(string value)
        {
            if (value.Contains(',') || value.Contains('"'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        private static string GetSuggestedAction(string group)
            => group switch
            {
                "Product Code mismatches" => "Please verify the correct product was invoiced.",
                "Date mismatches" => "Review and confirm correct invoice date.",
                "Quantity mismatches" => "Investigate reason for quantity difference.",
                _ => string.Empty
            };
    }

    /// <summary>
    /// Represents a single discrepancy between two rows.
    /// </summary>
    public record Discrepancy(int Row, string Column, string LeftValue, string RightValue, string Explanation);
}
