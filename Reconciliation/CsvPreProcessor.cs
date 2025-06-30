using System;
using System.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        // Apply header aliases
        var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "SubscriptionGuid", "SubscriptionId" },
            { "DomainUrl", "CustomerDomainName" },
            { "CustomerName", "CustomerDomainName" },
            { "CustomerId", "CustomerDomainName" },
            { "ProductGuid", "ProductId" },
            { "MPNId", "ProductId" },
            { "BillableQuantity", "Quantity" }
        };

        foreach (var kvp in aliasMap)
            Rename(table, kvp.Key, kvp.Value);

        Rename(table, "PartnerUnitPrice", "UnitPrice");
        Rename(table, "PartnerEffectiveUnitPrice", "EffectiveUnitPrice");
        Rename(table, "PartnerTotal", "Total");
        Rename(table, "PartnerSubTotal", "Subtotal");
        Rename(table, "MSRP", "MSRPPrice");

        foreach (var col in new[] { "UnitPrice", "EffectiveUnitPrice", "Subtotal", "TaxTotal", "Total" })
        {
            if (!table.Columns.Contains(col))
                table.Columns.Add(col, typeof(string));
        }

        // Normalise key fields (trim, upper, date format)
        DataNormaliser.Normalise(table, new[]
        {
            "CustomerDomainName","ProductId","ChargeType","ChargeStartDate",
            "SubscriptionId","SubscriptionGuid"
        });
    }

    private static void Rename(DataTable table, string oldName, string newName)
    {
        if (!table.Columns.Contains(oldName)) return;
        if (table.Columns.Contains(newName)) table.Columns.Remove(newName);
        table.Columns[oldName].ColumnName = newName;
    }
}
