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
            lock (_errors)
            {
                _errors.Add(message);
            }
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
