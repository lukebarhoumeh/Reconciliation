using System;
using System.Data;
using System.Collections.Generic;

namespace Reconciliation;

/// <summary>
/// Normalises column names across partner and Microsoft invoices so that
/// financial fields can be compared directly.
/// </summary>
public static class CsvPreProcessor
{
    /// <summary>
    /// Apply standard column mappings and ensure required financial columns exist.
    /// </summary>
    public static void Process(DataTable table)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));

        // --- Aliases for all IDs and financial columns (applies to both tables) ---
        var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // --- IDs (for key matching) ---
            { "SubscriptionGuid", "SubscriptionId" },
            { "SubId",           "SubscriptionId" },
            { "CustomerName",    "CustomerDomainName" },
            { "CustomerId",      "CustomerDomainName" },
            { "DomainUrl",       "CustomerDomainName" },
            { "ProductGuid",     "ProductId" },
            { "MPNId",           "ProductId" },
            { "PartNumber",      "ProductId" },
            // --- Financial fields ---
            { "MSRP",                      "MSRPPrice" },
            { "BillableQuantity",          "Quantity" }
        };

        foreach (var kvp in aliasMap)
            MergeColumn(table, kvp.Key, kvp.Value);

        // Ensure all needed columns exist for downstream logic
        foreach (var col in new[] {
            "UnitPrice", "EffectiveUnitPrice", "Subtotal", "TaxTotal", "Total", "MSRPPrice",
            "Quantity", "SubscriptionId", "ProductId", "CustomerDomainName", "ChargeType", "ChargeStartDate"
        })
        {
            if (!table.Columns.Contains(col))
                table.Columns.Add(col, typeof(string));
        }

        // Normalise key fields: upper-case, trimmed, and charge dates to yyyy-MM-dd
        foreach (DataRow row in table.Rows)
        {
            foreach (var key in new[] { "CustomerDomainName", "ProductId", "ChargeType", "SubscriptionId" })
            {
                if (table.Columns.Contains(key) && row[key] != null)
                    row[key] = row[key].ToString()!.Trim().ToUpperInvariant();
            }
            // Normalize ChargeStartDate for reliable keying
            if (table.Columns.Contains("ChargeStartDate") && row["ChargeStartDate"] != null)
            {
                if (DateTime.TryParse(row["ChargeStartDate"].ToString(), out var d))
                    row["ChargeStartDate"] = d.ToString("yyyy-MM-dd");
                else
                    row["ChargeStartDate"] = row["ChargeStartDate"].ToString()!.Trim();
            }
        }
    }

    private static void MergeColumn(DataTable table, string oldName, string newName)
    {
        if (!table.Columns.Contains(oldName)) return;

        if (!table.Columns.Contains(newName))
        {
            table.Columns[oldName].ColumnName = newName;
            return;
        }

        foreach (DataRow row in table.Rows)
        {
            string target = Convert.ToString(row[newName]) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(target))
                row[newName] = row[oldName];
        }

        table.Columns.Remove(oldName);
    }
}
