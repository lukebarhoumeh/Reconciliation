using System.Data;
using System.Linq;

namespace Reconciliation
{
    internal static class DataTableExtensions
    {
        /// <summary>
        /// Attempt to rename a column in <paramref name="table"/> to <paramref name="expected"/> using fuzzy matching.
        /// </summary>
        /// <param name="table">Table to modify.</param>
        /// <param name="expected">Expected column name.</param>
        /// <returns>True if a column was renamed.</returns>
        public static bool TryFuzzyRenameColumn(this DataTable table, string expected)
        {
            var options = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
            var match = FuzzyMatcher.FindClosest(expected, options);
            if (match != null && match != expected)
            {
                table.Columns[match].ColumnName = expected;
                ErrorLogger.LogWarning(-1, expected,
                    $"Column '{expected}' was not found. Using close match '{match}'.",
                    match, string.Empty, string.Empty);
                return true;
            }
            return table.Columns.Contains(expected);
        }
    }
}
