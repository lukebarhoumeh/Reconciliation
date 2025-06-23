using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Reconciliation
{
    public static class SchemaValidator
    {
        public static void RequireColumns(DataTable table, string fileLabel, IEnumerable<string> requiredColumns)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (requiredColumns == null) throw new ArgumentNullException(nameof(requiredColumns));

            var missing = requiredColumns.Where(c => !table.Columns.Contains(c)).ToArray();
            foreach (var col in missing)
                ErrorLogger.LogMissingColumn(col, fileLabel);

            if (missing.Length > 0)
                throw new ArgumentException($"{fileLabel} is missing required columns: {string.Join(", ", missing)}");
        }
    }
}
