using System;
using System.Data;
using System.Globalization;

namespace Reconciliation
{
    /// <summary>
    /// Provides invoice reconciliation between MSP Hub and Microsoft invoices.
    /// </summary>
    public class ReconciliationService
    {
        public string LastSummary { get; private set; } = string.Empty;

        /// <summary>
        /// Returns rows where the financial fields differ. Only UnitPrice,
        /// Quantity, Subtotal, TaxTotal and Total are compared and rows are
        /// matched using InvoiceNumber + SkuId.
        /// </summary>
        public DataTable CompareInvoices(DataTable sixDotOne, DataTable microsoft)
        {
            if (sixDotOne == null) throw new ArgumentNullException(nameof(sixDotOne));
            if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));

            var fields = new[] { "UnitPrice", "Quantity", "Subtotal", "TaxTotal", "Total" };

            // Duplicate InvoiceNumber/SkuId combinations may exist in the invoice
            // files, so we group rows by this composite key and compare lists of
            // rows rather than assuming a single match on each side.
            var hub = sixDotOne.Rows.Cast<DataRow>()
                .GroupBy(r => Key(r), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var ms = microsoft.Rows.Cast<DataRow>()
                .GroupBy(r => Key(r), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var result = BuildResultTable();
            int row = 1;

            foreach (var key in hub.Keys.Union(ms.Keys, StringComparer.OrdinalIgnoreCase))
            {
                hub.TryGetValue(key, out var hubRows);
                ms.TryGetValue(key, out var msRows);

                if (hubRows == null)
                {
                    AddMissingRow(result, row++, "Missing in MSPHub", key);
                    continue;
                }
                if (msRows == null)
                {
                    AddMissingRow(result, row++, "Missing in Microsoft", key);
                    continue;
                }

                foreach (var hubRow in hubRows)
                {
                    int hubIndex = hubRow.Table.Rows.IndexOf(hubRow) + 1;
                    foreach (var msRow in msRows)
                    {
                        int msIndex = msRow.Table.Rows.IndexOf(msRow) + 1;
                        foreach (var f in fields)
                        {
                            if (!hubRow.Table.Columns.Contains(f) || !msRow.Table.Columns.Contains(f))
                                continue;

                            string a = Convert.ToString(hubRow[f]) ?? string.Empty;
                            string b = Convert.ToString(msRow[f]) ?? string.Empty;
                            if (ValuesEqual(a, b))
                                continue;

                            var r = result.NewRow();
                            r["Row Number"] = row++;
                            r["Field Name"] = FriendlyNameMap.Get(f);
                            r["Our Value"] = a;
                            r["Microsoft Value"] = b;
                            r["Explanation"] = $"Mismatch in {f} (Hub row {hubIndex}, MS row {msIndex})";
                            r["Suggested Action"] = string.Empty;
                            r["Reason"] = string.Empty;
                            result.Rows.Add(r);
                        }
                    }
                }
            }

            LastSummary = $"Discrepancies found: {result.Rows.Count}";
            return result;
        }

        private static DataTable BuildResultTable()
        {
            var t = new DataTable();
            t.Columns.Add("Row Number", typeof(int));
            t.Columns.Add("Field Name", typeof(string));
            t.Columns.Add("Our Value", typeof(string));
            t.Columns.Add("Microsoft Value", typeof(string));
            t.Columns.Add("Explanation", typeof(string));
            t.Columns.Add("Suggested Action", typeof(string));
            t.Columns.Add("Reason", typeof(string));
            return t;
        }

        private static void AddMissingRow(DataTable table, int row, string message, string key)
        {
            var r = table.NewRow();
            r["Row Number"] = row;
            r["Field Name"] = "Row";
            r["Our Value"] = key;
            r["Microsoft Value"] = string.Empty;
            r["Explanation"] = message;
            r["Suggested Action"] = string.Empty;
            r["Reason"] = message;
            table.Rows.Add(r);
        }

        public static bool TryParseMoney(string s, out decimal d)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                d = 0m;
                return true;
            }
            return decimal.TryParse(
                s,
                NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowExponent |
                NumberStyles.AllowTrailingSign,
                CultureInfo.InvariantCulture,
                out d);
        }

        private static string Key(DataRow row)
        {
            string invoice = row.Table.Columns.Contains("InvoiceNumber")
                ? Convert.ToString(row["InvoiceNumber"]) ?? string.Empty
                : string.Empty;
            string sku = row.Table.Columns.Contains("SkuId")
                ? Convert.ToString(row["SkuId"]) ?? string.Empty
                : string.Empty;
            return (invoice + "|" + sku).ToUpperInvariant();
        }

        private static bool ValuesEqual(string a, string b)
        {
            a = a.Trim();
            b = b.Trim();
            if (TryParseMoney(a, out var da) && TryParseMoney(b, out var db))
            {
                return Math.Abs(da - db) <= AppConfig.Validation.NumericTolerance;
            }
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
