using System.Data;
using System.Globalization;

namespace Reconciliation;

/// <summary>
/// Extension methods for DataTable operations.
/// </summary>
public static class DataTableExtensions
{
    /// <summary>
    /// Sets a decimal value using invariant culture formatting.
    /// </summary>
    public static void SetColumnValue(this DataTable table, DataRow row, string column, decimal value)
    {
        row[column] = value.ToString(CultureInfo.InvariantCulture);
    }
}
