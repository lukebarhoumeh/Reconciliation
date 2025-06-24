using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace Reconciliation;

public static class DataNormaliser
{
    public static void Normalise(DataTable table, IEnumerable<string> keyColumns)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        var keys = new HashSet<string>(keyColumns, StringComparer.OrdinalIgnoreCase);
        foreach (DataRow row in table.Rows)
        {
            foreach (DataColumn col in table.Columns)
            {
                string value = Convert.ToString(row[col]) ?? string.Empty;
                value = value.Trim();
                if (keys.Contains(col.ColumnName))
                    value = value.ToUpperInvariant();
                if (DateTime.TryParse(value, out var date))
                {
                    row[col] = date.Date.ToString("yyyy-MM-dd");
                    continue;
                }
                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                {
                    row[col] = dec.ToString(CultureInfo.InvariantCulture);
                    continue;
                }
                row[col] = value;
            }
        }
    }
}
