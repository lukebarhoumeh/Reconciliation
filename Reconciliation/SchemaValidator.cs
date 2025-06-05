using System;
using System.Collections.Generic;
using System.Data;

namespace Reconciliation
{
    public static class SchemaValidator
    {
        public static void RequireColumns(DataTable table, string fileName, IEnumerable<string> required, bool allowFuzzy)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (required == null) throw new ArgumentNullException(nameof(required));

            foreach (var column in required)
            {
                if (table.Columns.Contains(column))
                    continue;

                if (allowFuzzy && table.TryFuzzyRenameColumn(column))
                    continue;

                ErrorLogger.LogMissingColumn(column, fileName);
                throw new ArgumentException($"The expected column '{column}' is missing from the {fileName} file.");
            }
        }
    }
}
