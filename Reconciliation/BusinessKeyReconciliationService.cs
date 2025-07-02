using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Reconciliation
{
    /// <summary>
    /// Reconciles two invoice tables using a minimal business key:
    /// CustomerDomainName + ProductId. All other columns – ChargeType,
    /// SubscriptionId/Guid, dates, etc. – are compared only after a key
    /// match has been found.
    /// </summary>
    public class BusinessKeyReconciliationService
    {
        private static readonly (string Hub, string Ms)[] FinancialColumns =
        {
            ("Quantity",         "Quantity"),
            ("PartnerUnitPrice", "UnitPrice"),
            ("PartnerSubTotal",  "Subtotal"),
            ("PartnerTotal",     "Total")
        };

        public string LastSummary { get; private set; } = string.Empty;

        // ------------------------------------------------------------------
        //  Public entry point
        // ------------------------------------------------------------------
        public DataTable Reconcile(DataTable msphub, DataTable microsoft)
        {
            if (msphub == null) throw new ArgumentNullException(nameof(msphub));
            if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));

            CsvPreProcessor.Process(msphub);
            CsvPreProcessor.Process(microsoft);

            var hubGroups = BuildGroups(msphub);
            var msGroups = BuildGroups(microsoft);

            var result = BuildResultTable();
            int matched = 0, missing = 0, mismatched = 0;

            var sharedFields = FinancialColumns
                .Where(p => msphub.Columns.Contains(p.Hub) && microsoft.Columns.Contains(p.Ms))
                .ToArray();

            foreach (var key in hubGroups.Keys)
            {
                var ours = hubGroups[key];
                if (!msGroups.TryGetValue(key, out var theirs))
                {
                    AddSimpleRow(result, key, "Missing in Microsoft");
                    missing++;
                    continue;
                }

                bool allEqual = true;
                foreach (var field in sharedFields)
                {
                    decimal oursTotal = ours.Sum(r => ValueParser.SafeDecimal(r[field.Hub]));
                    decimal msTotal = theirs.Sum(r => ValueParser.SafeDecimal(r[field.Ms]));
                    if (Math.Abs(oursTotal - msTotal) > AppConfig.Validation.NumericTolerance)
                    {
                        allEqual = false;
                        break;
                    }
                }

                if (allEqual)
                {
                    matched++;
                }
                else
                {
                    AddSimpleRow(result, key, "Mismatched");
                    mismatched++;
                }
            }

            LastSummary = $"Matched: {matched} | Missing in Microsoft: {missing} | Mismatched: {mismatched}";
            return result;
        }

        // ==================================================================
        //  Helpers
        // ==================================================================
        #region Group‑key builders
        private Dictionary<string, List<DataRow>> BuildGroups(DataTable table)
        {
            var dict = new Dictionary<string, List<DataRow>>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in table.Rows)
            {
                if (!TryBuildGroupKey(row, out string key))
                {
                    int idx = table.Rows.IndexOf(row) + 1;
                    SimpleLogger.Warn($"Skipping row {idx}: missing CustomerDomainName or ProductId");
                    continue;
                }

                if (!dict.TryGetValue(key, out var list))
                {
                    list = new List<DataRow>();
                    dict[key] = list;
                }
                list.Add(row);
            }
            return dict;
        }

        /// <summary>
        /// Key used only for grouping. It deliberately excludes ChargeType,
        /// ChargeStartDate and SubscriptionId/Guid because those values often
        /// change between systems and would otherwise block legitimate matches.
        /// </summary>
        private static bool TryBuildGroupKey(DataRow row, out string key)
        {
            string Customer()
            {
                if (row.Table.Columns.Contains("CustomerDomainName"))
                {
                    string v = Convert.ToString(row["CustomerDomainName"]) ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
                if (row.Table.Columns.Contains("CustomerName"))
                    return Convert.ToString(row["CustomerName"]) ?? string.Empty;
                return string.Empty;
            }

            string Product()
            {
                if (row.Table.Columns.Contains("ProductId"))
                {
                    string v = Convert.ToString(row["ProductId"]) ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
                if (row.Table.Columns.Contains("PartNumber"))
                    return Convert.ToString(row["PartNumber"]) ?? string.Empty;
                return string.Empty;
            }

            string customer = Customer().Trim();
            string product = Product().Trim();

            if (string.IsNullOrWhiteSpace(customer) || string.IsNullOrWhiteSpace(product))
            {
                key = string.Empty;
                return false;
            }

            key = string.Join("|", customer.ToUpperInvariant(), product.ToUpperInvariant());
            return true;
        }

        #endregion

        #region Result‑table helpers
        private static DataTable BuildResultTable()
        {
            var t = new DataTable();
            t.Columns.Add("CustomerDomainName");
            t.Columns.Add("ProductId");
            t.Columns.Add("Status");
            return t;
        }

        private static void AddSimpleRow(DataTable table, string key, string status)
        {
            var r = table.NewRow();
            var parts = key.Split('|');
            r["CustomerDomainName"] = parts.Length > 0 ? parts[0] : string.Empty;
            r["ProductId"] = parts.Length > 1 ? parts[1] : string.Empty;
            r["Status"] = status;
            table.Rows.Add(r);
        }
        #endregion
    }
}
