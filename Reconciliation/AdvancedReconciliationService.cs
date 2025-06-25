using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Reconciliation;

/// <summary>
/// Order‑agnostic reconciliation engine.
/// Aggregates each CSV on the composite key, compares money / quantity with
/// tolerances, and emits a flat discrepancy table ready for UI binding.
/// </summary>
public class AdvancedReconciliationService
{
    // ── Config values pulled once for speed ──────────────────────────────────
    private readonly decimal _tolAmount = AppConfig.Reconciliation.ToleranceAmount;
    private readonly decimal _tolQuantity = AppConfig.Reconciliation.ToleranceQuantity;
    private readonly decimal _highPriCut = AppConfig.Reconciliation.HighPriorityThreshold;
    private readonly IReadOnlyList<string> _keyCols = AppConfig.Reconciliation.CompositeKeys;

    // ── Public façade ────────────────────────────────────────────────────────
    public virtual ReconciliationResult Reconcile(DataTable microsoft, DataTable partner)
    {
        if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));
        if (partner == null) throw new ArgumentNullException(nameof(partner));

        var msGroups = BuildGroups(microsoft, out var msDuplicates);
        var ptGroups = BuildGroups(partner, out var ptDuplicates);

        var resultTable = BuildResultTable();
        int discrepancyCount = 0;
        decimal over = 0m, under = 0m;

        // Compare every key that appears in either file
        foreach (var key in msGroups.Keys.Union(ptGroups.Keys))
        {
            msGroups.TryGetValue(key, out var ms);
            ptGroups.TryGetValue(key, out var pt);

            if (ms == null)
            {
                AddRow(resultTable, key, "MISSING_IN_MICROSOFT", null, pt,
                       highPriority: true);
                discrepancyCount++;
                over += pt!.Total;
                continue;
            }

            if (pt == null)
            {
                AddRow(resultTable, key, "MISSING_IN_PARTNER", ms, null,
                       highPriority: true);
                discrepancyCount++;
                under += ms.Total;
                continue;
            }

            bool added = false;

            // Total mismatch
            if (Math.Abs(ms.Total - pt.Total) > _tolAmount)
            {
                AddRow(resultTable, key, "TOTAL_MISMATCH", ms, pt,
                       highPriority: Math.Abs(ms.Total - pt.Total) >= _highPriCut);
                discrepancyCount++;
                added = true;
            }

            // Quantity mismatch
            if (Math.Abs(ms.Quantity - pt.Quantity) > _tolQuantity)
            {
                AddRow(resultTable, key,
                       added ? "QUANTITY_MISMATCH (same key)" : "QUANTITY_MISMATCH",
                       ms, pt, highPriority: false);
                // count only once per key (already counted if totals mismatched)
                if (!added) discrepancyCount++;
            }
        }

        // Duplicates are always high‑priority
        foreach (var dupKey in msDuplicates.Concat(ptDuplicates).Distinct())
            AddRow(resultTable, dupKey, "DUPLICATE_KEY", null, null, highPriority: true);

        var summary = new ReconciliationSummary(
            microsoft.Rows.Count + partner.Rows.Count,
            discrepancyCount, over, under);

        return new ReconciliationResult(resultTable, summary);
    }

    // ── Internal helpers ─────────────────────────────────────────────────────
    private sealed class Stats
    {
        public decimal Quantity;
        public decimal Total;
        public bool ContainsRefund;   // used for priority flag
    }

    /// <summary>
    /// Groups rows by the composite key and aggregates quantity/total.
    /// </summary>
    private Dictionary<string, Stats> BuildGroups(
        DataTable table, out List<string> duplicateKeys)
    {
        var dict = new Dictionary<string, Stats>(StringComparer.OrdinalIgnoreCase);
        var dups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (DataRow row in table.Rows)
        {
            var key = string.Join("|",
                        _keyCols.Select(k => (row[k]?.ToString() ?? string.Empty).Trim()));

            if (!dict.TryGetValue(key, out var stats))
            {
                stats = new Stats();
                dict[key] = stats;
            }
            else
            {
                dups.Add(key); // record duplicate
            }

            stats.Quantity += ParseDecimal(row["Quantity"]);
            stats.Total += EffectiveTotal(row);
            if (string.Equals(row["ChargeType"]?.ToString(), "Refund",
                              StringComparison.OrdinalIgnoreCase))
                stats.ContainsRefund = true;
        }

        duplicateKeys = dups.ToList();
        return dict;
    }

    private static decimal EffectiveTotal(DataRow row)
    {
        decimal eup = ParseDecimal(row["EffectiveUnitPrice"]);
        decimal qty = ParseDecimal(row["Quantity"]);
        decimal total = ParseDecimal(row["Total"]);

        return eup != 0m ? eup * qty : total;
    }

    private static decimal ParseDecimal(object? value)
    {
        if (value == null) return 0m;
        decimal.TryParse(Convert.ToString(value), NumberStyles.Any,
                         CultureInfo.InvariantCulture, out var d);
        return d;
    }

    /// <summary>Creates a shell table with all columns strongly typed as string.</summary>
    private static DataTable BuildResultTable()
    {
        var t = new DataTable();
        t.Columns.Add("Key");
        t.Columns.Add("Discrepancy");
        t.Columns.Add("MicrosoftTotal");
        t.Columns.Add("PartnerTotal");
        t.Columns.Add("MicrosoftQty");
        t.Columns.Add("PartnerQty");
        t.Columns.Add("IsHighPriority");   // new column expected by UI
        return t;
    }

    private static void AddRow(
        DataTable table, string key, string type,
        Stats? ms, Stats? pt, bool highPriority)
    {
        var r = table.NewRow();
        r["Key"] = key;
        r["Discrepancy"] = type;
        r["MicrosoftTotal"] = ms?.Total.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        r["PartnerTotal"] = pt?.Total.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        r["MicrosoftQty"] = ms?.Quantity.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        r["PartnerQty"] = pt?.Quantity.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        r["IsHighPriority"] = highPriority;
        table.Rows.Add(r);
    }
}
