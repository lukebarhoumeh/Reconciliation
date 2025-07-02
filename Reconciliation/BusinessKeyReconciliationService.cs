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
        private static readonly string[] FinancialColumns =
        {
            "UnitPrice", "EffectiveUnitPrice", "Subtotal",
            "TaxTotal",  "Total",             "Quantity"
        };

        public string LastSummary { get; private set; } = string.Empty;

        // ------------------------------------------------------------------
        //  Public entry point
        // ------------------------------------------------------------------
        public DataTable Reconcile(DataTable ours, DataTable microsoft)
        {
            if (ours == null) throw new ArgumentNullException(nameof(ours));
            if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));

            //---------------------------------------------------------------
            // 1. Tenant filter: keep only MS rows for our PartnerId
            //---------------------------------------------------------------
            string partnerId = string.Empty;
            if (ours.Columns.Contains("PartnerId"))
            {
                partnerId = ours.AsEnumerable()
                                .Select(r => Convert.ToString(r["PartnerId"]) ?? string.Empty)
                                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(partnerId) && microsoft.Columns.Contains("PartnerId"))
            {
                var msRows = microsoft.AsEnumerable()
                    .Where(r => string.Equals(Convert.ToString(r["PartnerId"]),
                                              partnerId,
                                              StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                microsoft = msRows.Length > 0 ? msRows.CopyToDataTable()
                                              : microsoft.Clone();
            }

            //---------------------------------------------------------------
            // 2. Pre‑process both tables (normalises dates, numbers, etc.)
            //---------------------------------------------------------------
            CsvPreProcessor.Process(ours);
            CsvPreProcessor.Process(microsoft);

            //---------------------------------------------------------------
            // 3. Filter Microsoft rows by the tenant domains we have
            //---------------------------------------------------------------
            var tenantSet = ours.AsEnumerable()
                                .Select(r => r["CustomerDomainName"]?.ToString()?.Trim()
                                              ?.ToUpperInvariant())
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToHashSet();

            if (microsoft.Rows.Count > 0 && microsoft.Columns.Contains("CustomerDomainName"))
            {
                var msRows = microsoft.AsEnumerable()
                    .Where(r => tenantSet.Contains(r["CustomerDomainName"]
                                                   ?.ToString()
                                                   ?.Trim()
                                                   ?.ToUpperInvariant()))
                    .ToArray();

                microsoft = msRows.Length > 0 ? msRows.CopyToDataTable()
                                              : microsoft.Clone();
            }

            //---------------------------------------------------------------
            // 4. Group the two tables by the *reduced* business key
            //---------------------------------------------------------------
            var oursGroups = BuildGroups(ours);
            var msGroups = BuildGroups(microsoft);

            //---------------------------------------------------------------
            // 5. Determine which financial columns both tables share
            //---------------------------------------------------------------
            var sharedFields = FinancialColumns
                               .Where(c => ours.Columns.Contains(c) &&
                                           microsoft.Columns.Contains(c))
                               .ToArray();

            if (sharedFields.Length == 0)
                SimpleLogger.Info("No shared finance columns – row‑presence only.");

            //---------------------------------------------------------------
            // 6. Core reconciliation loop
            //---------------------------------------------------------------
            var result = BuildResultTable();
            int onlyOur = 0, onlyMs = 0, mismatches = 0, perfect = 0;

            foreach (var key in oursGroups.Keys.Union(msGroups.Keys,
                                                      StringComparer.OrdinalIgnoreCase))
            {
                oursGroups.TryGetValue(key, out var ourRows);
                msGroups.TryGetValue(key, out var msRowsList);

                // -------- Only in Microsoft --------------------------------
                if (ourRows == null)
                {
                    foreach (var row in msRowsList!)
                    {
                        AddMissingRow(result, BuildFullKey(row), "Missing in MSP‑Hub");
                        onlyMs++;
                    }
                    continue;
                }

                // -------- Only in MSP‑Hub ----------------------------------
                if (msRowsList == null)
                {
                    foreach (var row in ourRows)
                    {
                        AddMissingRow(result, BuildFullKey(row), "Missing in Microsoft");
                        onlyOur++;
                    }
                    continue;
                }

                // -------- Matched key – aggregate financials ----------------
                bool rowMismatch = false;
                foreach (var field in sharedFields)
                {
                    decimal oursTotal = ourRows.Sum(r => SafeDecimal(r[field]));
                    decimal msTotal = msRowsList.Sum(r => SafeDecimal(r[field]));

                    if (Math.Abs(oursTotal - msTotal) <= AppConfig.Validation.NumericTolerance)
                        continue;

                    AddMismatchRow(result, BuildFullKey(ourRows[0]), field,
                                   oursTotal.ToString(CultureInfo.InvariantCulture),
                                   msTotal.ToString(CultureInfo.InvariantCulture));
                    mismatches++;
                    rowMismatch = true;
                }

                if (!rowMismatch) perfect++;
            }

            //---------------------------------------------------------------
            // 7. Summary & return
            //---------------------------------------------------------------
            LastSummary = $"Perfect:{perfect} | Only-MSP:{onlyOur} | " +
                          $"Only-MS:{onlyMs} | Diff:{mismatches}";
            SimpleLogger.Info($"Tenant {partnerId}: {LastSummary}");
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
                if (!HasBasicKey(row)) continue;   // skip rows with missing essentials

                string key = BuildGroupKey(row);
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
        private static string BuildGroupKey(DataRow row)
        {
            string V(string col) => row.Table.Columns.Contains(col)
                ? (row[col]?.ToString() ?? string.Empty).Trim().ToUpperInvariant()
                : string.Empty;

            return string.Join("|", V("CustomerDomainName"), V("ProductId"));
        }

        /// <summary>
        /// Key that is shown in the result grid.  We keep all the original
        /// fields here for context, even though they do not participate in
        /// the group key.
        /// </summary>
        private static string BuildFullKey(DataRow row)
        {
            string V(string col) => row.Table.Columns.Contains(col)
                ? (row[col]?.ToString() ?? string.Empty).Trim().ToUpperInvariant()
                : string.Empty;

            string sub = V("SubscriptionId");
            if (string.IsNullOrEmpty(sub) && row.Table.Columns.Contains("SubscriptionGuid"))
                sub = V("SubscriptionGuid");

            string date = V("ChargeStartDate");

            return string.Join("|",
                V("CustomerDomainName"),
                V("ProductId"),
                V("ChargeType"),
                date,
                sub);
        }
        #endregion

        #region Row‑level utilities
        private static bool HasBasicKey(DataRow row) =>
            new[] { "CustomerDomainName", "ProductId" }
            .All(c => row.Table.Columns.Contains(c) &&
                      !string.IsNullOrWhiteSpace(Convert.ToString(row[c])));

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
        #endregion

        #region Result‑table helpers
        private static DataTable BuildResultTable()
        {
            var t = new DataTable();
            foreach (var c in new[]
                     { "CustomerDomainName", "ProductId", "ChargeType",
                       "ChargeStartDate",   "SubscriptionId" })
                t.Columns.Add(c);

            t.Columns.Add("Field Name");
            t.Columns.Add("Our Value");
            t.Columns.Add("Microsoft Value");
            t.Columns.Add("Explanation");
            t.Columns.Add("Suggested Action");
            t.Columns.Add("Reason");
            return t;
        }

        private static void AddMissingRow(DataTable table,
                                          string key,
                                          string message)
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
            r["Reason"] = "Row missing in " +
                                    (message.Contains("Microsoft")
                                         ? "Microsoft invoice"
                                         : "MSP‑Hub invoice");
            table.Rows.Add(r);
        }

        private static void AddMismatchRow(DataTable table,
                                           string key,
                                           string field,
                                           string ourVal,
                                           string msVal)
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
        #endregion
    }
}
