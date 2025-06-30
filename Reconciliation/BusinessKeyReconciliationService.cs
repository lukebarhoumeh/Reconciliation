using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Reconciliation;

/// <summary>
/// Provides strict business key reconciliation between two invoices.
/// Keys are matched on CustomerDomainName, ProductId, ChargeType,
/// ChargeStartDate (±1 day tolerance) and SubscriptionId. Only financial
/// columns present in both tables are compared.
/// </summary>
public class BusinessKeyReconciliationService
{
    private readonly string[] _keyColumns =
    {
        "CustomerDomainName",
        "ProductId",
        "ChargeType",
        "ChargeStartDate",
        "SubscriptionId"
    };

    private static readonly string[] FinancialColumns =
    {
        "UnitPrice","EffectiveUnitPrice","Subtotal","TaxTotal","Total","Quantity"
    };

    /// <summary>Summary of the last reconciliation run.</summary>
    public string LastSummary { get; private set; } = string.Empty;

    /// <summary>
    /// Reconcile two invoice tables.
    /// </summary>
    public DataTable Reconcile(DataTable ours, DataTable microsoft)
    {
        if (ours == null) throw new ArgumentNullException(nameof(ours));
        if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));

        CsvPreProcessor.Process(ours);
        CsvPreProcessor.Process(microsoft);

        var oursGroups = BuildGroups(ours);
        var msGroups = BuildGroups(microsoft);

        var sharedFields = FinancialColumns.Where(f =>
                ours.Columns.Contains(f) && microsoft.Columns.Contains(f))
            .ToArray();

        var result = BuildResultTable();
        int onlyMsphub = 0, onlyMicrosoft = 0, mismatchCount = 0, perfect = 0;

        foreach (var key in oursGroups.Keys.Union(msGroups.Keys))
        {
            oursGroups.TryGetValue(key, out var oursRows);
            msGroups.TryGetValue(key, out var msRows);

            if (oursRows == null)
            {
                foreach (var msr in msRows!)
                {
                    AddMissingRow(result, BuildFullKey(msr), "Missing in MSPUP");
                    onlyMicrosoft++;
                }
                continue;
            }
            if (msRows == null)
            {
                foreach (var or in oursRows)
                {
                    AddMissingRow(result, BuildFullKey(or), "Missing in Microsoft");
                    onlyMsphub++;
                }
                continue;
            }

            var msRemaining = new List<DataRow>(msRows);
            foreach (var or in oursRows)
            {
                var oDate = ParseDate(or["ChargeStartDate"]);
                int idx = msRemaining.FindIndex(r => Math.Abs((ParseDate(r["ChargeStartDate"]) - oDate).Days) <= 1);
                if (idx == -1)
                {
                    AddMissingRow(result, BuildFullKey(or), "Missing in Microsoft");
                    onlyMsphub++;
                    continue;
                }

                var msr = msRemaining[idx];
                msRemaining.RemoveAt(idx);

                bool mismatch = false;
                foreach (var field in sharedFields)
                {
                    var a = Convert.ToString(or[field]) ?? string.Empty;
                    var b = Convert.ToString(msr[field]) ?? string.Empty;
                    if (ValuesEqual(a, b)) continue;
                    AddMismatchRow(result, BuildFullKey(or), field, a, b);
                    mismatchCount++;
                    mismatch = true;
                }
                if (!mismatch) perfect++;
            }
            foreach (var msr in msRemaining)
            {
                AddMissingRow(result, BuildFullKey(msr), "Missing in MSPUP");
                onlyMicrosoft++;
            }
        }

        LastSummary = $"Perfect: {perfect} | OnlyMSP: {onlyMsphub} | OnlyMS: {onlyMicrosoft} | Diff: {mismatchCount}";
        return result;
    }

    private Dictionary<string, List<DataRow>> BuildGroups(DataTable table)
    {
        var dict = new Dictionary<string, List<DataRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (DataRow row in table.Rows)
        {
            if (!HasValidKey(row)) continue;
            var key = BuildGroupKey(row);
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<DataRow>();
                dict[key] = list;
            }
            list.Add(row);
        }
        return dict;
    }

    private string BuildGroupKey(DataRow row)
    {
        return string.Join("|", new[]{
            Value(row,"CustomerDomainName"),
            Value(row,"ProductId"),
            Value(row,"ChargeType"),
            SubscriptionValue(row)
        });
    }

    private static string BuildFullKey(DataRow row)
    {
        return string.Join("|", new[]{
            Value(row,"CustomerDomainName"),
            Value(row,"ProductId"),
            Value(row,"ChargeType"),
            DateTime.TryParse(Convert.ToString(row["ChargeStartDate"]), out var d) ? d.ToString("yyyy-MM-dd") : Value(row,"ChargeStartDate"),
            SubscriptionValue(row)
        });
    }

    private static bool HasValidKey(DataRow row)
    {
        var basics = new[] { "CustomerDomainName", "ProductId", "ChargeType", "ChargeStartDate" }
            .All(c => row.Table.Columns.Contains(c) && !string.IsNullOrWhiteSpace(Convert.ToString(row[c])));
        if (!basics) return false;
        string sub = SubscriptionValue(row);
        return !string.IsNullOrEmpty(sub);
    }

    private static string SubscriptionValue(DataRow row)
    {
        string val = row.Table.Columns.Contains("SubscriptionId") ? Value(row,"SubscriptionId") : string.Empty;
        if (string.IsNullOrEmpty(val) && row.Table.Columns.Contains("SubscriptionGuid"))
            val = Value(row,"SubscriptionGuid");
        return val;
    }

    private static string Value(DataRow row, string column)
        => (Convert.ToString(row[column]) ?? string.Empty).Trim().ToUpperInvariant();

    private static DataTable BuildResultTable()
    {
        var t = new DataTable();
        foreach (var c in new[]{"CustomerDomainName","ProductId","ChargeType","ChargeStartDate","SubscriptionId"})
            t.Columns.Add(c);
        t.Columns.Add("Field Name");
        t.Columns.Add("Our Value");
        t.Columns.Add("Microsoft Value");
        t.Columns.Add("Explanation");
        t.Columns.Add("Suggested Action");
        t.Columns.Add("Reason");
        return t;
    }

    private static void AddMissingRow(DataTable table, string key, string message)
    {
        var r = table.NewRow();
        var parts = key.Split('|');
        for (int i = 0; i < 5; i++)
            r[i] = i < parts.Length ? parts[i] : string.Empty;
        r["Field Name"] = "Row";
        r["Our Value"] = string.Empty;
        r["Microsoft Value"] = string.Empty;
        r["Explanation"] = message;
        r["Suggested Action"] = string.Empty;
        r["Reason"] = "Row missing in " + (message.Contains("Microsoft") ? "Microsoft invoice" : "MSPUP invoice");
        table.Rows.Add(r);
    }

    private static void AddMismatchRow(DataTable table, string key, string field, string ourVal, string msVal)
    {
        var r = table.NewRow();
        var parts = key.Split('|');
        for (int i = 0; i < 5; i++)
            r[i] = i < parts.Length ? parts[i] : string.Empty;
        r["Field Name"] = FriendlyNameMap.Get(field);
        r["Our Value"] = ourVal;
        r["Microsoft Value"] = msVal;
        r["Explanation"] = $"Mismatch in {field}: {ourVal} vs {msVal}";
        r["Suggested Action"] = string.Empty;
        r["Reason"] = "Amount mismatch";
        table.Rows.Add(r);
    }

    private static bool ValuesEqual(string a, string b)
    {
        a = a.Trim();
        b = b.Trim();
        if (decimal.TryParse(a, NumberStyles.Any, CultureInfo.InvariantCulture, out var da) &&
            decimal.TryParse(b, NumberStyles.Any, CultureInfo.InvariantCulture, out var db))
            return Math.Abs(da - db) <= AppConfig.Validation.NumericTolerance;
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime ParseDate(object? val)
    {
        if (val == null) return DateTime.MinValue;
        if (DateTime.TryParse(Convert.ToString(val), out var d)) return d.Date;
        return DateTime.MinValue;
    }
}
