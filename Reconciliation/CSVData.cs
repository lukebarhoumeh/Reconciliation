using Microsoft.VisualBasic.FileIO;
using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace Reconciliation
{
    internal class CSVData
    {
        public static DataView GetCsvData(string filePath)
        {
            var dt = new DataTable();

            // 1) Read & trim headers + rows
            using (var p = new TextFieldParser(filePath))
            {
                p.SetDelimiters(",");
                p.HasFieldsEnclosedInQuotes = true;

                if (!p.EndOfData)
                {
                    var hdrs = p.ReadFields();
                    foreach (var h in hdrs)
                        dt.Columns.Add(h?.Trim() ?? "");
                }

                var dateCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "ValidFrom","ValidTo","OrderDate",
                    "SubscriptionStartDate","SubscriptionEndDate",
                    "ChargeStartDate","ChargeEndDate","PurchaseDate"
                };

                while (!p.EndOfData)
                {
                    var f = p.ReadFields();
                    var row = dt.NewRow();
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        var col = dt.Columns[i].ColumnName;
                        var raw = i < f.Length ? f[i] : "";
                        if (dateCols.Contains(col) &&
                            DateTime.TryParse(raw, out var d))
                        {
                            row[i] = d.ToString("MM/dd/yyyy");
                        }
                        else row[i] = raw;
                    }
                    dt.Rows.Add(row);
                }
            }

            // 2) Rename any Partner-prefix columns into your expected names
            Rename(dt, "PartnerUnitPrice", "UnitPrice");
            Rename(dt, "PartnerEffectiveUnitPrice", "EffectiveUnitPrice");
            Rename(dt, "MSRP", "MSRPPrice");

            // 3) Stub every column your compare needs
            Stub(dt, "PartnerDiscountPercentage");
            Stub(dt, "MSRPPrice");              // in case it never existed
            Stub(dt, "EffectiveMSRPPrice");
            Stub(dt, "AdminDiscountPercentage");

            // 4) Normalize your join keys
            Normalize(dt, "InvoiceNumber", "ChargeType", "Quantity");

            return dt.DefaultView;
        }

        static void Rename(DataTable dt, string oldName, string newName)
        {
            if (dt.Columns.Contains(oldName))
            {
                if (dt.Columns.Contains(newName))
                    dt.Columns.Remove(newName);
                dt.Columns[oldName].ColumnName = newName;
            }
        }

        static void Stub(DataTable dt, string name)
        {
            if (!dt.Columns.Contains(name))
                dt.Columns.Add(name, typeof(string));
        }

        static void Normalize(DataTable dt, params string[] keys)
        {
            foreach (DataRow r in dt.Rows)
                foreach (var k in keys)
                    if (dt.Columns.Contains(k))
                    {
                        var s = r[k]?.ToString() ?? "";
                        r[k] = new string(s.Trim()
                                           .ToUpperInvariant()
                                           .Where(c => !char.IsControl(c))
                                           .ToArray());
                    }
        }
    }
}
