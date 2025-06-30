using System;
using System.Data;
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
    }

    private static void Rename(DataTable table, string oldName, string newName)
    {
        if (!table.Columns.Contains(oldName)) return;
        if (table.Columns.Contains(newName)) table.Columns.Remove(newName);
        table.Columns[oldName].ColumnName = newName;
    }
}
