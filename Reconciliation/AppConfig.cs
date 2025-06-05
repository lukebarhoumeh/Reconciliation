using System;
using System.IO;
using System.Text.Json;

namespace Reconciliation
{
    /// <summary>
    /// Provides application configuration loaded from <c>appsettings.json</c>.
    /// </summary>
    public static class AppConfig
    {
        private static readonly ConfigRoot _root = Load();

        /// <summary>Validation-related options.</summary>
        public static ValidationOptions Validation => _root.Validation!;

        /// <summary>Logging-related options.</summary>
        public static LoggingOptions Logging => _root.Logging!;

        private static ConfigRoot Load()
        {
            var defaults = new ConfigRoot();
            try
            {
                string envPath = Environment.GetEnvironmentVariable("RECONCILIATION_CONFIG_PATH") ?? string.Empty;
                string path = !string.IsNullOrWhiteSpace(envPath)
                    ? envPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var root = JsonSerializer.Deserialize<ConfigRoot>(json);
                    if (root != null)
                    {
                        root.Validation ??= new ValidationOptions();
                        root.Logging ??= new LoggingOptions();
                        return root;
                    }
                }
            }
            catch
            {
                // ignore and use defaults
            }
            defaults.Validation ??= new ValidationOptions();
            defaults.Logging ??= new LoggingOptions();
            return defaults;
        }

        private class ConfigRoot
        {
            public ValidationOptions? Validation { get; set; }
            public LoggingOptions? Logging { get; set; }
        }
    }

    /// <summary>
    /// Options controlling tolerance values used during validation and comparison.
    /// </summary>
    public class ValidationOptions
    {
        public decimal NumericTolerance { get; set; } = 0.01m;
        public int DateToleranceDays { get; set; } = 0;
        public int TextDistance { get; set; } = 2;
        public decimal BlankThreshold { get; set; } = 0.1m;
    }

    /// <summary>
    /// Options controlling error log output.
    /// </summary>
    public class LoggingOptions
    {
        public int MaxDetailedRows { get; set; } = 5;
    }
}
