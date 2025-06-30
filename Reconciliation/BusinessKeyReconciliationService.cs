using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Reconciliation
{
    /// <summary>
    /// Reconciles two invoice tables using a fixed composite business key and
    /// compares only specified financial columns with tolerance.
    /// </summary>
    public class BusinessKeyReconciliationService
    {
        private static readonly string[] KeyColumns =
        {
            "CustomerDomainName",
            "ProductId",
            "ChargeType",
            "ChargeStartDate",
            "SubscriptionId"
        };

        private static readonly string[] FinancialColumns =
        {
            "UnitPrice","EffectiveUnitPrice","MSRP","MSRPPrice","Subtotal","TaxTotal","Total","Quantity",
            "PartnerDiscountPercentage","PartnerDiscount","PartnerSubTotal","PartnerTotal",
            "CustomerUnitPrice","CustomerEffectiveUnitPrice","CustomerSubTotal","CustomerTotal",
            "CustomerDiscountPercentage","CustomerDiscount",
            "EffectiveMSRP","PartnerUnitPrice","PartnerPerDayUnitPrice","CustomerPerDayUnitPrice"
        };

        /// <summary>Summary text describing how many discrepancies were found.</summary>
        public string LastSummary { get; private set; } = string.Empty;

        /// <summary>
        /// Compare two invoices and return a table of mismatched fields ordered
        /// by business key and field name.
        /// </summary>
        public DataTable Reconcile(DataTable ours, DataTable microsoft)
        {
            if (ours == null) throw new ArgumentNullException(nameof(ours));
            if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));

            var oursGroups = BuildGroups(ours);
            var msGroups = BuildGroups(microsoft);
            var sharedFields = FinancialColumns
                .Where(f => ours.Columns.Contains(f) && microsoft.Columns.Contains(f))
                .ToArray();

            var result = BuildResultTable();

            foreach (var key in oursGroups.Keys.Union(msGroups.Keys))
            {
                oursGroups.TryGetValue(key, out var oursRows);
                msGroups.TryGetValue(key, out var msRows);
                int count = Math.Max(oursRows?.Count ?? 0, msRows?.Count ?? 0);
                for (int i = 0; i < count; i++)
                {
                    DataRow? ourRow = (oursRows != null && i < oursRows.Count) ? oursRows[i] : null;
                    DataRow? msRow = (msRows != null && i < msRows.Count) ? msRows[i] : null;
                    if (ourRow == null || msRow == null)
                    {
                        AddMissingRow(result, key, ourRow == null ? "Missing in MSPUP" : "Missing in Microsoft");
                        continue;
                    }
                    foreach (var field in sharedFields)
                    {
                        var ourVal = Convert.ToString(ourRow[field]) ?? string.Empty;
                        var msVal = Convert.ToString(msRow[field]) ?? string.Empty;
                        if (ValuesEqual(ourVal, msVal)) continue;
                        AddMismatchRow(result, key, field, ourVal, msVal);
                    }
                }
            }

            // Order by composite key then field name
            var ordered = result.AsEnumerable()
                .OrderBy(r => r[KeyColumns[0]])
                .ThenBy(r => r[KeyColumns[1]])
                .ThenBy(r => r[KeyColumns[2]])
                .ThenBy(r => r[KeyColumns[3]])
                .ThenBy(r => r[KeyColumns[4]])
                .ThenBy(r => r["Field Name"]);
            var orderedTable = BuildResultTable();
            foreach (var r in ordered) orderedTable.ImportRow(r);

            LastSummary = $"Discrepancies found: {orderedTable.Rows.Count}";
            return orderedTable;
        }

        private static Dictionary<string, List<DataRow>> BuildGroups(DataTable table)
        {
            var dict = new Dictionary<string, List<DataRow>>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in table.Rows)
            {
                if (!HasValidKey(row)) continue;
                string key = BuildKey(row);
                if (!dict.TryGetValue(key, out var list))
                {
                    list = new List<DataRow>();
                    dict[key] = list;
                }
                list.Add(row);
            }
            return dict;
        }

        private static bool HasValidKey(DataRow row)
        {
            foreach (var col in KeyColumns)
            {
                if (!row.Table.Columns.Contains(col)) return false;
                string val = Convert.ToString(row[col]) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(val)) return false;
            }
            return true;
        }

        private static string BuildKey(DataRow row)
        {
            return string.Join("|", KeyColumns.Select(c => Convert.ToString(row[c])!.Trim().ToUpperInvariant()));
        }

        private static DataTable BuildResultTable()
        {
            var t = new DataTable();
            foreach (var c in KeyColumns) t.Columns.Add(c);
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
            for (int i = 0; i < KeyColumns.Length; i++)
                r[KeyColumns[i]] = i < parts.Length ? parts[i] : string.Empty;
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
            for (int i = 0; i < KeyColumns.Length; i++)
                r[KeyColumns[i]] = i < parts.Length ? parts[i] : string.Empty;
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
            {
                return Math.Abs(da - db) <= AppConfig.Validation.NumericTolerance;
            }
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
