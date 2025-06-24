using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Reconciliation;

public class AdvancedReconciliationService
{
    private decimal ToleranceAmount => AppConfig.Reconciliation.ToleranceAmount;
    private decimal ToleranceQuantity => AppConfig.Reconciliation.ToleranceQuantity;
    private IReadOnlyList<string> Keys => AppConfig.Reconciliation.CompositeKeys;

    public ReconciliationResult Reconcile(DataTable msft, DataTable other)
    {
        if (msft == null) throw new ArgumentNullException(nameof(msft));
        if (other == null) throw new ArgumentNullException(nameof(other));

        var msGroups = BuildGroups(msft, out var msDup);
        var otherGroups = BuildGroups(other, out var otherDup);

        DataTable table = new();
        table.Columns.Add("Key");
        table.Columns.Add("Discrepancy");
        table.Columns.Add("MicrosoftTotal");
        table.Columns.Add("PartnerTotal");
        table.Columns.Add("MicrosoftQty");
        table.Columns.Add("PartnerQty");

        int unmatched = 0;
        decimal over = 0m;
        decimal under = 0m;

        var allKeys = new HashSet<string>(msGroups.Keys.Concat(otherGroups.Keys));
        foreach (var key in allKeys)
        {
            msGroups.TryGetValue(key, out var msStats);
            otherGroups.TryGetValue(key, out var ptStats);

            if (msStats == null)
            {
                AddRow(table, key, "MISSING_IN_MICROSOFT", null, ptStats);
                unmatched++;
                over += ptStats.Total;
                continue;
            }
            if (ptStats == null)
            {
                AddRow(table, key, "MISSING_IN_PARTNER", msStats, null);
                unmatched++;
                under += msStats.Total;
                continue;
            }
            if (Math.Abs(msStats.Total - ptStats.Total) > ToleranceAmount)
            {
                AddRow(table, key, "TOTAL_MISMATCH", msStats, ptStats);
                unmatched++;
            }
            if (Math.Abs(msStats.Quantity - ptStats.Quantity) > ToleranceQuantity)
            {
                AddRow(table, key, "QUANTITY_MISMATCH", msStats, ptStats);
                unmatched++;
            }
        }

        foreach (var d in msDup.Concat(otherDup))
            AddRow(table, d, "DUPLICATE_KEY", null, null);

        var summary = new ReconciliationSummary(msft.Rows.Count + other.Rows.Count, unmatched, over, under);
        return new ReconciliationResult(table, summary);
    }

    private class Stats
    {
        public decimal Quantity;
        public decimal Total;
    }

    private Dictionary<string, Stats> BuildGroups(DataTable table, out List<string> duplicates)
    {
        var dict = new Dictionary<string, Stats>();
        duplicates = new List<string>();
        foreach (DataRow row in table.Rows)
        {
            string key = string.Join("|", Keys.Select(k => Convert.ToString(row[k]) ?? string.Empty));
            if (dict.ContainsKey(key))
                duplicates.Add(key);
            if (!dict.TryGetValue(key, out var stats))
            {
                stats = new Stats();
                dict[key] = stats;
            }
            stats.Quantity += ParseDecimal(row["Quantity"]);
            decimal eup = ParseDecimal(row["EffectiveUnitPrice"]);
            decimal total = eup != 0m ? eup * ParseDecimal(row["Quantity"]) : ParseDecimal(row["Total"]);
            stats.Total += total;
        }
        return dict;
    }

    private static decimal ParseDecimal(object? obj)
    {
        if (obj == null) return 0m;
        decimal.TryParse(Convert.ToString(obj), NumberStyles.Any, CultureInfo.InvariantCulture, out var d);
        return d;
    }

    private static void AddRow(DataTable table, string key, string type, Stats? ms, Stats? pt)
    {
        var r = table.NewRow();
        r["Key"] = key;
        r["Discrepancy"] = type;
        r["MicrosoftTotal"] = ms?.Total.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        r["PartnerTotal"] = pt?.Total.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        r["MicrosoftQty"] = ms?.Quantity.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        r["PartnerQty"] = pt?.Quantity.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        table.Rows.Add(r);
    }
}
