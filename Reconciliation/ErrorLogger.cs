using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Reconciliation
{
    public static class ErrorLogger
    {
        private static readonly List<string> _errors = new();
        private static readonly List<string> _warnings = new();

        private static readonly Dictionary<string, int> _errorCounts = new();
        private static readonly Dictionary<string, int> _warningCounts = new();

        public static IReadOnlyList<string> Errors => _errors.AsReadOnly();
        public static IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

        public static IReadOnlyDictionary<string, int> ErrorSummary => _errorCounts;
        public static IReadOnlyDictionary<string, int> WarningSummary => _warningCounts;

        public static bool HasErrors => _errors.Count > 0;
        public static bool HasWarnings => _warnings.Count > 0;

        public static void LogError(string message)
        {
            Add(_errors, _errorCounts, message);
        }

        public static void LogWarning(string message)
        {
            Add(_warnings, _warningCounts, message);
        }

        public static void LogMissingColumn(string column, string file)
        {
            string msg = $"The expected column '{column}' was not found in the {file} file. Please check your CSV for missing or renamed columns.";
            LogError(msg);
        }

        private static void Add(List<string> list, Dictionary<string, int> counts, string message)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            lock (list)
            {
                list.Add(entry);
            }
            lock (counts)
            {
                if (counts.ContainsKey(message))
                    counts[message]++;
                else
                    counts[message] = 1;
            }
        }

        public static void Clear()
        {
            lock (_errors)
            {
                _errors.Clear();
                _errorCounts.Clear();
            }
            lock (_warnings)
            {
                _warnings.Clear();
                _warningCounts.Clear();
            }
        }

        public static void Export(string filePath)
        {
            var lines = new List<string>
            {
                $"Total Errors: {ErrorSummary.Values.Sum()}, Total Warnings: {WarningSummary.Values.Sum()}",
                "Type,Message,Count"
            };

            foreach (var kv in ErrorSummary)
            {
                lines.Add($"Error,\"{kv.Key}\",{kv.Value}");
            }

            foreach (var kv in WarningSummary)
            {
                lines.Add($"Warning,\"{kv.Key}\",{kv.Value}");
            }

            File.WriteAllLines(filePath, lines);
        }
    }
}
