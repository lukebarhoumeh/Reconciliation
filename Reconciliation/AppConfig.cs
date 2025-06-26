using System;
using System.IO;
using System.Text.Json;

namespace Reconciliation;

/// <summary>
/// Strongly‑typed wrapper around <c>appsettings.json</c>.
/// Callers access <see cref="Validation"/>, <see cref="Logging"/>, or
/// <see cref="Reconciliation"/>; the underlying JSON is loaded once on demand.
/// </summary>
public static class AppConfig
{
    // Thread‑safe lazy singleton
    private static readonly Lazy<ConfigRoot> _root = new(Load, isThreadSafe: true);

    public static ValidationOptions Validation => _root.Value.Validation;
    public static LoggingOptions Logging => _root.Value.Logging;
    public static ReconciliationOptions Reconciliation => _root.Value.Reconciliation;

    // ─────────────────────────────────────────────────────────────────────────
    //  Internal: JSON loader
    // ─────────────────────────────────────────────────────────────────────────
    private static ConfigRoot Load()
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        try
        {
            if (File.Exists(filePath))
            {
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var root = JsonSerializer.Deserialize<ConfigRoot>(File.ReadAllText(filePath), opts);
                if (root is not null) return root.WithDefaults();
            }
        }
        catch (Exception ex)
        {
            // Non‑fatal: fall back to defaults but emit to stderr for visibility
            Console.Error.WriteLine($"[AppConfig] Failed to load {filePath}: {ex.Message}");
        }

        // All fallbacks / error cases land here
        return new ConfigRoot().WithDefaults();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  DTOs
    // ─────────────────────────────────────────────────────────────────────────
    private sealed class ConfigRoot
    {
        public ValidationOptions? Validation { get; set; }
        public LoggingOptions? Logging { get; set; }
        public ReconciliationOptions? Reconciliation { get; set; }

        // Ensures nested objects are never null
        public ConfigRoot WithDefaults()
        {
            Validation ??= new ValidationOptions();
            Logging ??= new LoggingOptions();
            Reconciliation ??= new ReconciliationOptions();
            return this;
        }
    }
}

/// <summary>Options for data‑quality validation.</summary>
public class ValidationOptions
{
    public decimal NumericTolerance { get; set; } = 0.01m;
    public int DateToleranceDays { get; set; } = 0;
    public int TextDistance { get; set; } = 2;
    public decimal BlankThreshold { get; set; } = 0.1m;
}

/// <summary>Options controlling error‑log verbosity.</summary>
public class LoggingOptions
{
    public int MaxDetailedRows { get; set; } = 5;
}

/// <summary>
/// Advanced‑reconciliation knobs.  Defaults match the new engine.
/// </summary>
public class ReconciliationOptions
{
    public decimal ToleranceAmount { get; set; } = 0.01m;
    public decimal ToleranceQuantity { get; set; } = 0.01m;
    /// <summary>Absolute delta (in pricing currency) that marks a discrepancy as high‑priority.</summary>
    public decimal HighPriorityThreshold { get; set; } = 20m;
    public decimal PerDayUnitTolerance { get; set; } = 0.05m;

    /// <summary>Columns that compose the unique business key for grouping.</summary>
    public string[] CompositeKeys { get; set; } =
        { "CustomerDomainName","ProductId","SkuId","ChargeType","Term","BillingCycle","ReferenceId" };

    /// <summary>Relative or absolute path to the column‑alias map.</summary>
    public string ColumnMapPath { get; set; } = "column-map.json";
}