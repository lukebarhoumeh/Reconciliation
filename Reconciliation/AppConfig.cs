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

        /// <summary>Advanced reconciliation options.</summary>
        public static ReconciliationOptions Reconciliation => _root.Reconciliation!;

        private static ConfigRoot Load()
        {
            var defaults = new ConfigRoot();
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var root = JsonSerializer.Deserialize<ConfigRoot>(json);
                    if (root != null)
                    {
                        root.Validation ??= new ValidationOptions();
                        root.Logging ??= new LoggingOptions();
                        root.Reconciliation ??= new ReconciliationOptions();
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
            defaults.Reconciliation ??= new ReconciliationOptions();
            return defaults;
        }

        private class ConfigRoot
        {
            public ValidationOptions? Validation { get; set; }
            public LoggingOptions? Logging { get; set; }
            public ReconciliationOptions? Reconciliation { get; set; }
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

    /// <summary>
    /// Options controlling advanced reconciliation.
    /// </summary>
    public class ReconciliationOptions
    {
        public decimal ToleranceAmount { get; set; } = 0.01m;
        public decimal ToleranceQuantity { get; set; } = 0.01m;
        public string[] CompositeKeys { get; set; } = Array.Empty<string>();
        public string ColumnMapPath { get; set; } = "column-map.json";
    }
}
