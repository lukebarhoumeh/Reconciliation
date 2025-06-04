using System;
using System.Collections.Generic;
using System.IO;

namespace Reconciliation
{
    public static class ErrorLogger
    {
        private static readonly List<string> _errors = new();

        public static IReadOnlyList<string> Errors => _errors.AsReadOnly();

        public static bool HasErrors => _errors.Count > 0;

        public static void Log(string message)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            lock (_errors)
            {
                _errors.Add(entry);
            }
        }

        public static void LogMissingColumn(string column, string file)
        {
            string msg = $"The expected column '{column}' was not found in the {file} file. Please check your CSV for missing or renamed columns.";
            Log(msg);
        }

        public static void Clear()
        {
            lock (_errors)
            {
                _errors.Clear();
            }
        }

        public static void Export(string filePath)
        {
            File.WriteAllLines(filePath, _errors);
        }
    }
}
