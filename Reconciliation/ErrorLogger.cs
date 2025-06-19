using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace Reconciliation
{
    public record ErrorLogEntry(DateTime Timestamp, string ErrorLevel, int RowNumber,
        string ColumnName, string Description, string RawValue, string FileName, string Context,
        bool IsSummary = false);

    public static class ErrorLogger
    {
        private static readonly List<ErrorLogEntry> _entries = new();
        private static readonly List<ErrorLogEntry> _allEntries = new();
        private static readonly Dictionary<string, int> _errorCounts = new();
        private static readonly Dictionary<string, int> _warningCounts = new();
        private static readonly Dictionary<string, int> _detailCounts = new();
        private static readonly Dictionary<string, ErrorLogEntry> _summaries = new();

        static ErrorLogger()
        {
            MaxDetailedRows = AppConfig.Logging.MaxDetailedRows;
        }

        public static int MaxDetailedRows { get; set; } = 5;

        public static IReadOnlyList<ErrorLogEntry> Entries => _entries.AsReadOnly();
        public static IReadOnlyList<ErrorLogEntry> AllEntries => _allEntries.AsReadOnly();
        public static IReadOnlyDictionary<string, int> ErrorSummary => _errorCounts;
        public static IReadOnlyDictionary<string, int> WarningSummary => _warningCounts;

        public static bool HasErrors => _entries.Any(e => e.ErrorLevel == "Error");
        public static bool HasWarnings => _entries.Any(e => e.ErrorLevel == "Warning");

        public static void LogError(int rowNumber, string columnName, string description,
            string rawValue, string fileName, string context)
        {
            Add("Error", rowNumber, columnName, description, rawValue, fileName, context);
        }

        public static void LogWarning(int rowNumber, string columnName, string description,
            string rawValue, string fileName, string context)
        {
            Add("Warning", rowNumber, columnName, description, rawValue, fileName, context);
        }

        public static void LogMissingColumn(string column, string file)
        {
            string msg = $"The expected column '{column}' was not found in the {file} file. Please check your CSV for missing or renamed columns.";
            LogError(-1, column, msg, string.Empty, file, string.Empty);
        }

        private static void Add(string level, int row, string column, string description,
            string rawValue, string fileName, string context)
        {
            rawValue = string.IsNullOrWhiteSpace(rawValue) ? string.Empty : rawValue;
            var entry = new ErrorLogEntry(DateTime.Now, level, row, column, description, rawValue, fileName, context);
            lock (_allEntries)
            {
                _allEntries.Add(entry);
            }
            string key = $"{level}|{column}|{description}";
            int count;
            lock (_detailCounts)
            {
                _detailCounts.TryGetValue(key, out count);
                count++;
                _detailCounts[key] = count;
            }

            lock (_entries)
            {
                if (count <= MaxDetailedRows)
                {
                    _entries.Add(entry);
                }
                else
                {
                    if (!_summaries.TryGetValue(key, out var summary))
                    {
                        summary = new ErrorLogEntry(DateTime.Now, level, -1, column,
                            $"1 additional rows had the same error: {description}",
                            string.Empty, fileName, string.Empty, true);
                        _summaries[key] = summary;
                        _entries.Add(summary);
                    }
                    else
                    {
                        int idx = _entries.IndexOf(summary);
                        summary = summary with
                        {
                            Timestamp = DateTime.Now,
                            Description = $"{count - MaxDetailedRows} additional rows had the same error: {description}"
                        };
                        _entries[idx] = summary;
                        _summaries[key] = summary;
                    }
                }
            }
            var counts = level == "Error" ? _errorCounts : _warningCounts;
            lock (counts)
            {
                if (counts.ContainsKey(description))
                    counts[description]++;
                else
                    counts[description] = 1;
            }
        }

        public static void Clear()
        {
            lock (_entries)
            {
                _entries.Clear();
                _allEntries.Clear();
                _errorCounts.Clear();
                _warningCounts.Clear();
                _detailCounts.Clear();
                _summaries.Clear();
            }
        }

        public static void Export(string filePath)
        {
            var lines = new List<string>
            {
                "Timestamp,ErrorLevel,RowNumber,ColumnName,Description,RawValue,FileName,Context"
            };
            List<ErrorLogEntry> snapshot;
            lock (_allEntries)
            {
                snapshot = _allEntries.ToList();
            }
            foreach (var e in snapshot)
            {
                var ts = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                string desc = FormatNumeric(e.Description, e.ColumnName);
                string raw = FormatNumeric(e.RawValue, e.ColumnName);
                lines.Add(string.Join(',',
                    ts,
                    e.ErrorLevel,
                    e.RowNumber > 0 ? e.RowNumber.ToString() : "",
                    Escape(e.ColumnName),
                    Escape(desc),
                    Escape(raw),
                    Escape(e.FileName),
                    Escape(e.Context)));
            }
            File.WriteAllLines(filePath, lines);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Contains(',') || value.Contains('"')
                ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
        }

        private static string FormatNumeric(string value, string columnHint)
        {
            if (decimal.TryParse(value.TrimEnd('%'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                bool percent = columnHint.Contains("Percent", StringComparison.OrdinalIgnoreCase) || value.Trim().EndsWith("%");
                string formatted = percent ? NumericFormatter.Percent(d) : NumericFormatter.Money(d);
                return percent && value.Trim().EndsWith("%") ? formatted + "%" : formatted;
            }
            return value;
        }
    }
}
