using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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

                var options = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                var suggestion = FuzzyMatcher.FindClosest(column, options, 4);
                ErrorLogger.LogMissingColumn(column, fileName);
                var message = $"The expected column '{column}' is missing from the {fileName} file.";
                if (suggestion != null)
                    message += $" Did you mean '{suggestion}'?";
                throw new ArgumentException(message);
            }
        }
    }
}
