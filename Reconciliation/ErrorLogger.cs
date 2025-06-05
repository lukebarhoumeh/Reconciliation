using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Reconciliation
{
    public record ErrorLogEntry(DateTime Timestamp, string ErrorLevel, int RowNumber,
        string ColumnName, string Description, string RawValue, string FileName, string Context);

    public static class ErrorLogger
    {
        private static readonly List<ErrorLogEntry> _entries = new();
        private static readonly Dictionary<string, int> _errorCounts = new();
        private static readonly Dictionary<string, int> _warningCounts = new();
        private static readonly Dictionary<string, int> _detailCounts = new();
        private static readonly Dictionary<string, ErrorLogEntry> _summaries = new();

        public static int MaxDetailedRows { get; set; } = 5;

        public static IReadOnlyList<ErrorLogEntry> Entries => _entries.AsReadOnly();
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
            var entry = new ErrorLogEntry(DateTime.Now, level, row, column, description, rawValue, fileName, context);
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
                            $"1 additional rows with the same error in column {column}",
                            string.Empty, fileName, string.Empty);
                        _summaries[key] = summary;
                        _entries.Add(summary);
                    }
                    else
                    {
                        int idx = _entries.IndexOf(summary);
                        summary = summary with
                        {
                            Timestamp = DateTime.Now,
                            Description = $"{count - MaxDetailedRows} additional rows with the same error in column {column}"
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
            int err;
            int warn;
            lock (_entries)
            {
                foreach (var e in _entries)
                {
                    lines.Add(string.Join(',',
                        e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        e.ErrorLevel,
                        e.RowNumber > 0 ? e.RowNumber.ToString() : "-",
                        Escape(e.ColumnName),
                        Escape(e.Description),
                        Escape(e.RawValue),
                        Escape(e.FileName),
                        Escape(e.Context)));
                }
                err = _entries.Count(x => x.ErrorLevel == "Error");
                warn = _entries.Count(x => x.ErrorLevel == "Warning");
            }
            lines.Add(string.Join(',', new string[8]) + $",Total Errors: {err}, Total Warnings: {warn}");
            File.WriteAllLines(filePath, lines);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Contains(',') || value.Contains('"')
                ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
        }
    }
}
