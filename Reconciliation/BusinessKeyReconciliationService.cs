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

        private sealed class GroupTotals
        {
            public string CustomerDomain { get; init; } = string.Empty;
            public string ProductId { get; init; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Total { get; set; }
            // Stores a quantity-weighted sum of unit prices for averaging
            public decimal UnitPrice { get; set; }
            public decimal TaxTotal { get; set; }
            public bool HasError { get; set; }
        }

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

            // Filter Microsoft rows by PartnerId only if both sides contain a single matching ID
            if (msphub.Columns.Contains("PartnerId") && microsoft.Columns.Contains("PartnerId"))
            {
                var hubIds = msphub.AsEnumerable()
                                    .Select(r => Convert.ToString(r["PartnerId"]) ?? string.Empty)
                                    .Where(v => !string.IsNullOrWhiteSpace(v))
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToArray();
                var msIds = microsoft.AsEnumerable()
                                   .Select(r => Convert.ToString(r["PartnerId"]) ?? string.Empty)
                                   .Where(v => !string.IsNullOrWhiteSpace(v))
                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                   .ToArray();

                if (hubIds.Length == 1 && msIds.Length == 1 &&
                    string.Equals(hubIds[0], msIds[0], StringComparison.OrdinalIgnoreCase))
                {
                    string pid = hubIds[0];
                    var msRows = microsoft.AsEnumerable()
                        .Where(r => string.Equals(Convert.ToString(r["PartnerId"]), pid, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    microsoft = msRows.Length > 0 ? msRows.CopyToDataTable() : microsoft.Clone();
                }
                else
                {
                    SimpleLogger.Warn("PartnerId mismatch between files - skipping filter.");
                }
            }

            var hubGroups = Aggregate(msphub, microsoft: false);
            var msGroups  = Aggregate(microsoft, microsoft: true);

            var result = BuildResultTable();
            int matched = 0, missingMs = 0, missingHub = 0, mismatched = 0, errors = 0;

            SimpleLogger.Info($"Aggregated MSPHub keys: {hubGroups.Count}; MS keys: {msGroups.Count}");

            var allKeys = new HashSet<string>(hubGroups.Keys, StringComparer.OrdinalIgnoreCase);
            allKeys.UnionWith(msGroups.Keys);

            foreach (var key in allKeys)
            {
                hubGroups.TryGetValue(key, out var ours);
                msGroups.TryGetValue(key, out var theirs);

                if (ours == null)
                {
                    if (theirs != null)
                    {
                        AddMissingInHubRow(result, theirs);
                        SimpleLogger.Warn($"Key {key} missing in MSPHub results");
                        missingHub++;
                    }
                    continue;
                }

                if (ours.HasError)
                {
                    AddDataErrorRow(result, ours);
                    SimpleLogger.Warn($"Key {key} has missing CustomerDomainName or ProductId");
                    errors++;
                    continue;
                }

                if (theirs == null)
                {
                    AddMissingRow(result, ours);
                    SimpleLogger.Warn($"Key {key} missing in Microsoft results");
                    missingMs++;
                    continue;
                }

                if (TotalsEqual(ours, theirs))
                {
                    AddMatchRow(result, ours, theirs);
                    matched++;
                }
                else
                {
                    AddMismatchRow(result, ours, theirs);
                    mismatched++;
                }
            }

            LastSummary = $"Matched: {matched} | Missing in Microsoft: {missingMs} | Missing in MSPHub: {missingHub} | Mismatched: {mismatched} | Data Errors: {errors}";
            return result;
        }

        // ==================================================================
        //  Helpers
        // ==================================================================
        #region Group‑key builders
        private Dictionary<string, GroupTotals> Aggregate(DataTable table, bool microsoft)
        {
            var groups = new Dictionary<string, GroupTotals>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow row in table.Rows)
            {
                string cust = row.Table.Columns.Contains("CustomerDomainName")
                    ? Convert.ToString(row["CustomerDomainName"]) ?? string.Empty
                    : string.Empty;
                string prod = row.Table.Columns.Contains("ProductId")
                    ? Convert.ToString(row["ProductId"]) ?? string.Empty
                    : string.Empty;

                cust = cust.Trim();
                prod = prod.Trim();

                string key = string.Join("|", cust.ToUpperInvariant(), prod.ToUpperInvariant());

                if (!groups.TryGetValue(key, out var totals))
                {
                    totals = new GroupTotals { CustomerDomain = cust, ProductId = prod };
                    groups[key] = totals;
                }

                if (string.IsNullOrEmpty(cust) || string.IsNullOrEmpty(prod))
                    totals.HasError = true;

                decimal qty = SafeDecimal(row, "Quantity");
                totals.Quantity += qty;
                totals.Subtotal += SafeDecimal(row, "Subtotal", "PartnerSubTotal");
                totals.Total += SafeDecimal(row, "Total", "PartnerTotal");
                totals.UnitPrice += SafeDecimal(row, "UnitPrice", "PartnerUnitPrice") * qty;
                totals.TaxTotal += SafeDecimal(row, "TaxTotal", "PartnerTaxTotal");
            }

            return groups;
        }

        private static bool TotalsEqual(GroupTotals hub, GroupTotals ms)
        {
            decimal tol = AppConfig.Validation.NumericTolerance;
            return Math.Abs(hub.Quantity - ms.Quantity) <= tol &&
                   Math.Abs(hub.Subtotal - ms.Subtotal) <= tol &&
                   Math.Abs(hub.Total - ms.Total) <= tol &&
                   Math.Abs(hub.TaxTotal - ms.TaxTotal) <= tol;
        }

        private static decimal SafeDecimal(DataRow row, string column)
            => row.Table.Columns.Contains(column) ? ValueParser.SafeDecimal(row[column]) : 0m;

        private static decimal SafeDecimal(DataRow row, string colA, string colB)
        {
            if (row.Table.Columns.Contains(colA))
            {
                string raw = Convert.ToString(row[colA]) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(raw))
                    return ValueParser.SafeDecimal(raw);
            }
            if (row.Table.Columns.Contains(colB))
                return ValueParser.SafeDecimal(row[colB]);
            return 0m;
        }

        #endregion

        #region Result‑table helpers
        private static DataTable BuildResultTable()
        {
            var t = new DataTable();
            t.Columns.Add("CustomerDomainName");
            t.Columns.Add("ProductId");
            t.Columns.Add("Status");
            t.Columns.Add("HubQuantity", typeof(decimal));
            t.Columns.Add("MSQuantity", typeof(decimal));
            t.Columns.Add("HubSubtotal", typeof(decimal));
            t.Columns.Add("MSSubtotal", typeof(decimal));
            t.Columns.Add("HubTotal", typeof(decimal));
            t.Columns.Add("MSTotal", typeof(decimal));
            t.Columns.Add("HubUnitPrice", typeof(decimal));
            t.Columns.Add("MSUnitPrice", typeof(decimal));
            t.Columns.Add("HubTaxTotal", typeof(decimal));
            t.Columns.Add("MSTaxTotal", typeof(decimal));
            return t;
        }

        private static void AddMissingRow(DataTable table, GroupTotals ours)
        {
            var r = table.NewRow();
            r["CustomerDomainName"] = ours.CustomerDomain;
            r["ProductId"] = ours.ProductId;
            r["Status"] = "Missing in Microsoft";
            r["HubQuantity"] = ours.Quantity;
            r["MSQuantity"] = 0m;
            r["HubSubtotal"] = ours.Subtotal;
            r["MSSubtotal"] = 0m;
            r["HubTotal"] = ours.Total;
            r["MSTotal"] = 0m;
            r["HubUnitPrice"] = ours.Quantity == 0 ? 0m : ours.UnitPrice / ours.Quantity;
            r["MSUnitPrice"] = 0m;
            r["HubTaxTotal"] = ours.TaxTotal;
            r["MSTaxTotal"] = 0m;
            table.Rows.Add(r);
        }

        private static void AddMissingInHubRow(DataTable table, GroupTotals theirs)
        {
            var r = table.NewRow();
            r["CustomerDomainName"] = theirs.CustomerDomain;
            r["ProductId"] = theirs.ProductId;
            r["Status"] = "Missing in MSPHub";
            r["HubQuantity"] = 0m;
            r["MSQuantity"] = theirs.Quantity;
            r["HubSubtotal"] = 0m;
            r["MSSubtotal"] = theirs.Subtotal;
            r["HubTotal"] = 0m;
            r["MSTotal"] = theirs.Total;
            r["HubUnitPrice"] = 0m;
            r["MSUnitPrice"] = theirs.Quantity == 0 ? 0m : theirs.UnitPrice / theirs.Quantity;
            r["HubTaxTotal"] = 0m;
            r["MSTaxTotal"] = theirs.TaxTotal;
            table.Rows.Add(r);
        }

        private static void AddDataErrorRow(DataTable table, GroupTotals ours)
        {
            var r = table.NewRow();
            r["CustomerDomainName"] = ours.CustomerDomain;
            r["ProductId"] = ours.ProductId;
            r["Status"] = "Data Error";
            r["HubQuantity"] = ours.Quantity;
            r["MSQuantity"] = 0m;
            r["HubSubtotal"] = ours.Subtotal;
            r["MSSubtotal"] = 0m;
            r["HubTotal"] = ours.Total;
            r["MSTotal"] = 0m;
            r["HubUnitPrice"] = ours.Quantity == 0 ? 0m : ours.UnitPrice / ours.Quantity;
            r["MSUnitPrice"] = 0m;
            r["HubTaxTotal"] = ours.TaxTotal;
            r["MSTaxTotal"] = 0m;
            table.Rows.Add(r);
        }

        private static void AddMismatchRow(DataTable table, GroupTotals hub, GroupTotals ms)
        {
            var r = table.NewRow();
            r["CustomerDomainName"] = hub.CustomerDomain;
            r["ProductId"] = hub.ProductId;
            r["Status"] = "Mismatched";
            r["HubQuantity"] = hub.Quantity;
            r["MSQuantity"] = ms.Quantity;
            r["HubSubtotal"] = hub.Subtotal;
            r["MSSubtotal"] = ms.Subtotal;
            r["HubTotal"] = hub.Total;
            r["MSTotal"] = ms.Total;
            r["HubUnitPrice"] = hub.Quantity == 0 ? 0m : hub.UnitPrice / hub.Quantity;
            r["MSUnitPrice"] = ms.Quantity == 0 ? 0m : ms.UnitPrice / ms.Quantity;
            r["HubTaxTotal"] = hub.TaxTotal;
            r["MSTaxTotal"] = ms.TaxTotal;
            table.Rows.Add(r);
        }

        private static void AddMatchRow(DataTable table, GroupTotals hub, GroupTotals ms)
        {
            var r = table.NewRow();
            r["CustomerDomainName"] = hub.CustomerDomain;
            r["ProductId"] = hub.ProductId;
            r["Status"] = "Matched";
            r["HubQuantity"] = hub.Quantity;
            r["MSQuantity"] = ms.Quantity;
            r["HubSubtotal"] = hub.Subtotal;
            r["MSSubtotal"] = ms.Subtotal;
            r["HubTotal"] = hub.Total;
            r["MSTotal"] = ms.Total;
            r["HubUnitPrice"] = hub.Quantity == 0 ? 0m : hub.UnitPrice / hub.Quantity;
            r["MSUnitPrice"] = ms.Quantity == 0 ? 0m : ms.UnitPrice / ms.Quantity;
            r["HubTaxTotal"] = hub.TaxTotal;
            r["MSTaxTotal"] = ms.TaxTotal;
            table.Rows.Add(r);
        }
        #endregion
    }
}
