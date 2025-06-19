using System;
using System.Collections.Generic;

namespace Reconciliation
{
    internal static class FriendlyNameMap
    {
        private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SkuId"] = "Product Code",
            ["ChargeEndDate"] = "Invoice Date",
            ["CustomerDomainName"] = "Customer Website",
            ["PartnerId"] = "Partner ID",
            ["PartnerTaxTotal"] = "Partner Tax Amount"
        };

        public static string Get(string column)
            => Map.TryGetValue(column, out var val) ? val : column;
    }
}
