using System;
using System.Data;

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

            // Build lookups keyed by InvoiceNumber and SkuId (case-insensitive)
            var hub = sixDotOne.Rows.Cast<DataRow>().ToDictionary(
                r => Key(r), StringComparer.OrdinalIgnoreCase);
            var ms = microsoft.Rows.Cast<DataRow>().ToDictionary(
                r => Key(r), StringComparer.OrdinalIgnoreCase);

            var result = BuildResultTable();
            int row = 1;

            foreach (var key in hub.Keys.Union(ms.Keys))
            {
                hub.TryGetValue(key, out var hubRow);
                ms.TryGetValue(key, out var msRow);

                if (hubRow == null)
                {
                    AddMissingRow(result, row++, "Missing in MSPHub", key);
                    continue;
                }
                if (msRow == null)
                {
                    AddMissingRow(result, row++, "Missing in Microsoft", key);
                    continue;
                }

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
                    r["Explanation"] = $"Mismatch in {f}";
                    r["Suggested Action"] = string.Empty;
                    r["Reason"] = string.Empty;
                    result.Rows.Add(r);
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
            r["Field Name"] = key;
            r["Our Value"] = string.Empty;
            r["Microsoft Value"] = string.Empty;
            r["Explanation"] = message;
            r["Suggested Action"] = string.Empty;
            r["Reason"] = message;
            table.Rows.Add(r);
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
            if (decimal.TryParse(a, out var da) && decimal.TryParse(b, out var db))
            {
                return Math.Abs(da - db) <= AppConfig.Validation.NumericTolerance;
            }
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
