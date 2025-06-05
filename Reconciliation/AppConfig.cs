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
        /// <summary>Validation-related options.</summary>
        public static ValidationOptions Validation { get; } = Load();

        private static ValidationOptions Load()
        {
            var defaults = new ValidationOptions();
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var root = JsonSerializer.Deserialize<ConfigRoot>(json);
                    if (root?.Validation != null)
                        return root.Validation;
                }
            }
            catch
            {
                // ignore and use defaults
            }
            return defaults;
        }

        private class ConfigRoot
        {
            public ValidationOptions? Validation { get; set; }
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
}
