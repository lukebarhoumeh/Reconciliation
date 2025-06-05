using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace Reconciliation
{
    internal static class DataTableExtensions
    {
        private static readonly Dictionary<string, string[]> ColumnVariants = new()
        {
            ["SkuId"] = ["SkuName", "Sku", "SKU", "sku_id", "ItemNo", "Artikelnummer"],
            ["CustomerId"] = ["CustomerID", "Customer_Id", "CustomerNumber"],
            ["Quantity"] = ["QuantityOrdered", "Qty", "qty_ordered", "Quantité", "OrderQty"],
            ["ProductId"] = ["ProductID", "ProductCode", "ItemNo"],
            ["OrderDate"] = ["DateOrdered", "Order_Date"],
            ["CustomerDomainName"] = ["CustomerDomain", "DomainName"],
            ["BillingCycle"] = ["Billing_Cycle", "BillingPeriod"],
        };
        /// <summary>
        /// Attempt to rename a column in <paramref name="table"/> to <paramref name="expected"/> using fuzzy matching.
        /// </summary>
        /// <param name="table">Table to modify.</param>
        /// <param name="expected">Expected column name.</param>
        /// <returns>True if a column was renamed.</returns>
        public static bool TryFuzzyRenameColumn(this DataTable table, string expected)
        {
            var options = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            string? match = null;

            if (ColumnVariants.TryGetValue(expected, out var variants))
            {
                match = options.FirstOrDefault(o => variants.Any(v => FuzzyMatcher.IsFuzzyMatch(o, v)));
            }

            match ??= FuzzyMatcher.FindClosest(expected, options);

            if (match != null && match != expected)
            {
                table.Columns[match].ColumnName = expected;
                ErrorLogger.LogWarning(-1, expected,
                    $"Column '{match}' was mapped to required column '{expected}'.",
                    match, string.Empty, string.Empty);
                return true;
            }

            return table.Columns.Contains(expected);
        }
    }
}
