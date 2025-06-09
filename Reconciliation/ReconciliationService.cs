using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Reconciliation
{
    /// <summary>
    /// Provides invoice reconciliation between MSP Hub and Microsoft invoices.
    /// </summary>
    public class ReconciliationService
    {
        private readonly string[] _defaultKeyOrder =
        [
            "ProductId",
            "SkuId",
            "ChargeType",
            "Quantity",
            "SubscriptionStartDate",
            "SubscriptionEndDate",
            "Term",
            "BillingCycle",
        ];

        /// <summary>
        /// Compare MSP Hub and Microsoft invoices and return a mismatch table.
        /// </summary>
        /// <param name="msphub">MSP Hub invoice data.</param>
        /// <param name="microsoft">Microsoft invoice data.</param>
        /// <returns>Table of mismatched rows with reason columns.</returns>
        public DataTable CompareInvoices(DataTable msphub, DataTable microsoft)
        {
            return FindMismatchDetails(msphub, microsoft);
        }

        private DataTable FindMismatchDetails(DataTable sixdotOneDataTable, DataTable microsoftDataTable)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                LogComparisonStart();

                string[] columnsMspHubBilling = [
                    "CustomerName",
                    "CustomerDomainName",
                    "ProductId",
                    "ProductName",
                    "SkuId",
                    "ChargeType",
                    "SubscriptionStartDate",
                    "SubscriptionEndDate",
                    "EffectiveUnitPrice",
                    "Quantity",
                    "OrderDate",
                    "Term",
                    "BillingCycle",
                    "MSRPPrice",
                    "EffectiveMSRPPrice",
                    "ChargeStartDate",
                    "ChargeEndDate",
                    "PartnerEffectiveUnitPrice",
                    "AdminDiscountPercentage",
                    "PartnerDiscountPercentage",
                    "Total",
                ];

                string[] columnsMicrosoft = [
                    "CustomerName",
                    "CustomerDomainName",
                    "ProductId",
                    "ProductName",
                    "SkuId",
                    "ChargeType",
                    "SubscriptionStartDate",
                    "SubscriptionEndDate",
                    "EffectiveUnitPrice",
                    "Quantity",
                    "OrderDate",
                    "Term",
                    "BillingCycle",
                    "ChargeStartDate",
                    "ChargeEndDate",
                    "Total",
                ];

                var dateColumns = new HashSet<string>
                {
                    "ValidFrom",
                    "ValidTo",
                    "SubscriptionStartDate",
                    "SubscriptionEndDate",
                    "OrderDate",
                    "ChargeStartDate",
                    "ChargeEndDate",
                };
                var decimalColumns = new HashSet<string>
                {
                    "EffectiveUnitPrice",
                    "Quantity",
                    "MSRPPrice",
                    "PartnerEffectiveUnitPrice",
                    "AdminDiscountPercentage",
                    "PartnerDiscountPercentage",
                };

                DataTable mismatchTable = new DataTable();
                mismatchTable.Columns.Add(new DataColumn("Source", typeof(string)));
                foreach (var column in columnsMicrosoft.Union(columnsMspHubBilling))
                {
                    Type columnType = typeof(string);
                    if (decimalColumns.Contains(column))
                        columnType = typeof(decimal);
                    mismatchTable.Columns.Add(new DataColumn(column, columnType));
                }
                mismatchTable.Columns.Add(new DataColumn("Reason", typeof(string)));
                mismatchTable.Columns.Add(new DataColumn("90PercentOfAdminDiscPercent", typeof(decimal)));
                mismatchTable.Columns.Add(new DataColumn("Admin/PartnerDiscountCheck", typeof(string)));
                mismatchTable.Columns.Add(new DataColumn("EffectiveMSRPCheck", typeof(string)));
                mismatchTable.Columns.Add(new DataColumn("EffectiveUnitPriceCheck", typeof(string)));

                var keyOrder = _defaultKeyOrder;

                List<GroupedCustomerData> groupedMicrosoftData = new();
                List<GroupedCustomerData> groupedSixdotOneData = new();
                try
                {
                    var filteredMspBillingDataTable = sixdotOneDataTable.AsEnumerable()
                        .Where(row => SafeGetString(row, "ProductName") != "Azure plan" && SafeConvertToDecimal(row["UnitPrice"]) > 0)
                        .CopyToDataTable();

                    var filteredMicrosoftDataTable = microsoftDataTable.AsEnumerable()
                        .Where(row => SafeGetString(row, "SubscriptionDescription") != "Azure plan")
                        .CopyToDataTable();

                    groupedMicrosoftData = filteredMicrosoftDataTable.AsEnumerable()
                        .Where(row => !string.IsNullOrWhiteSpace(SafeGetString(row, "CustomerName")) &&
                                       !string.IsNullOrWhiteSpace(SafeGetString(row, "CustomerDomainName")))
                        .GroupBy(row => new
                        {
                            CustomerName = SafeGetString(row, "CustomerName"),
                            CustomerDomainName = SafeGetString(row, "CustomerDomainName"),
                        })
                        .OrderBy(group => group.Key.CustomerName)
                        .ThenBy(group => group.Key.CustomerDomainName)
                        .Select(group => new GroupedCustomerData
                        {
                            Key = $"{group.Key.CustomerName} - {group.Key.CustomerDomainName}",
                            Rows = group.OrderBy(row => string.Join("|", keyOrder.Select(k => SafeGetString(row, k)))).ToList(),
                        })
                        .ToList();

                    groupedSixdotOneData = filteredMspBillingDataTable.AsEnumerable()
                        .Where(row => !string.IsNullOrWhiteSpace(SafeGetString(row, "CustomerName")) &&
                                       !string.IsNullOrWhiteSpace(SafeGetString(row, "CustomerDomainName")))
                        .GroupBy(row => $"{SafeGetString(row, "CustomerName")} - {SafeGetString(row, "CustomerDomainName")}")
                        .OrderBy(group => group.Key)
                        .Select(group => new GroupedCustomerData
                        {
                            Key = group.Key,
                            Rows = group.OrderBy(row => string.Join("|", keyOrder.Select(k => SafeGetString(row, k)))).ToList(),
                        })
                        .ToList();
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError(-1, nameof(FindMismatchDetails), $"Error while filtering and grouping data: {ex.Message}", string.Empty, string.Empty, string.Empty);
                    return mismatchTable;
                }

                foreach (var microsoftGroup in groupedMicrosoftData)
                {
                    if (!microsoftGroup.Rows.Any())
                        continue;
                    string[] keyParts = microsoftGroup.Key.Split(" - ", StringSplitOptions.TrimEntries);
                    string customerName = keyParts.Length > 0 ? keyParts[0] : string.Empty;
                    string customerDomain = keyParts.Length > 1 ? keyParts[1] : string.Empty;

                    var sixdotOneGroup = groupedSixdotOneData.FirstOrDefault(g =>
                        g.Key.Contains(customerName, StringComparison.OrdinalIgnoreCase) ||
                        g.Key.Contains(customerDomain, StringComparison.OrdinalIgnoreCase));

                    if (sixdotOneGroup != null)
                    {
                        foreach (var microsoftRow in microsoftGroup.Rows)
                        {
                            try
                            {
                                var matchingSixdotOneRow = sixdotOneGroup.Rows.FirstOrDefault(msRow =>
                                    keyOrder.All(key => microsoftRow[key].ToString() == msRow[key].ToString()));
                                DataRow newRow = mismatchTable.NewRow();
                                newRow["Source"] = "Microsoft";

                                if (matchingSixdotOneRow == null)
                                {
                                    foreach (var column in columnsMicrosoft)
                                        newRow[column] = microsoftRow[column];
                                    newRow["Reason"] = "This Line Item is missing in MSP Hub Invoice.";
                                    mismatchTable.Rows.Add(newRow);
                                }
                                else
                                {
                                    // no business rules for equal rows (placeholder for future rules)
                                }
                            }
                            catch (Exception ex)
                            {
                                ErrorLogger.LogWarning(-1, nameof(FindMismatchDetails), $"Error processing Microsoft row: {ex.Message}", string.Empty, string.Empty, string.Empty);
                            }
                        }

                        foreach (var sixdotOneRow in sixdotOneGroup.Rows)
                        {
                            try
                            {
                                var matchingMicrosoftRow = microsoftGroup.Rows.FirstOrDefault(msRow =>
                                    keyOrder.All(key => sixdotOneRow[key].ToString() == msRow[key].ToString()));
                                DataRow newRow = mismatchTable.NewRow();
                                newRow["Source"] = "MSP Hub";
                                if (matchingMicrosoftRow != null)
                                {
                                    bool newRowRequired = false;
                                    decimal adminDiscount = SafeConvertToDecimal(sixdotOneRow["AdminDiscountPercentage"]);
                                    decimal calculatedAdminDiscount = Math.Round(CalculatePercentage(adminDiscount, 90), 2);
                                    decimal partnerDiscount = SafeConvertToDecimal(sixdotOneRow["PartnerDiscountPercentage"]);
                                    decimal effectiveMSRP = Math.Round(SafeConvertToDecimal(sixdotOneRow["EffectiveMSRPPrice"]), 8);
                                    decimal effectiveUnitPrice = Math.Round(SafeConvertToDecimal(sixdotOneRow["PartnerEffectiveUnitPrice"]), 8);
                                    int effectiveDays = Convert.ToInt32(sixdotOneRow["EffectiveDays"]);
                                    decimal msrpPrice = SafeConvertToDecimal(sixdotOneRow["MSRPPrice"]);
                                    decimal unitPrice = SafeConvertToDecimal(sixdotOneRow["PartnerUnitPrice"]);
                                    int totalDays = Convert.ToInt32(sixdotOneRow["TotalDays"]);
                                    decimal calculatedEffMSRP = Math.Round((msrpPrice * effectiveDays) / totalDays, 8);
                                    decimal calculatedEffUnitPrice = Math.Round((unitPrice * effectiveDays) / totalDays, 8);

                                    if (calculatedAdminDiscount != partnerDiscount)
                                    {
                                        newRowRequired = true;
                                        newRow["Admin/PartnerDiscountCheck"] = $"PartnerDiscountPercentage ({partnerDiscount}%) is not 90% of AdminDiscountPercentage.";
                                        newRow["90PercentOfAdminDiscPercent"] = calculatedAdminDiscount;
                                    }
                                    if (Math.Abs(Math.Round(effectiveMSRP, 2)) != Math.Round(calculatedEffMSRP, 2))
                                    {
                                        newRowRequired = true;
                                        newRow["EffectiveMSRPCheck"] = $"EffectiveMSRPPrice ({effectiveMSRP}) is not equal to CalculatedMSRPPrice ({calculatedEffMSRP}).";
                                    }
                                    if (Math.Round(effectiveUnitPrice, 2) != Math.Round(calculatedEffUnitPrice, 2))
                                    {
                                        newRowRequired = true;
                                        newRow["EffectiveUnitPriceCheck"] = $"PartnerEffectiveUnitPrice ({effectiveUnitPrice}) is not equal to CalculatedUnitPrice ({calculatedEffUnitPrice}).";
                                    }
                                    if (newRowRequired)
                                    {
                                        foreach (var column in columnsMspHubBilling)
                                            newRow[column] = sixdotOneRow[column];
                                        mismatchTable.Rows.Add(newRow);
                                    }
                                }
                                else
                                {
                                    foreach (var column in columnsMspHubBilling)
                                        newRow[column] = sixdotOneRow[column];
                                    newRow["Reason"] = "This Line Item is missing in Microsoft Invoice.";
                                    mismatchTable.Rows.Add(newRow);
                                }
                            }
                            catch (Exception ex)
                            {
                                ErrorLogger.LogWarning(-1, nameof(FindMismatchDetails), $"Error processing MSP Hub row: {ex.Message}", string.Empty, string.Empty, string.Empty);
                            }
                        }
                    }
                    else
                    {
                        foreach (var microsoftRow in microsoftGroup.Rows)
                        {
                            try
                            {
                                DataRow newRow = mismatchTable.NewRow();
                                newRow["Source"] = "Microsoft";
                                foreach (var column in columnsMicrosoft)
                                    newRow[column] = microsoftRow[column];
                                newRow["Reason"] = "This Line Item is missing in MSP Hub Invoice.";
                                mismatchTable.Rows.Add(newRow);
                            }
                            catch (Exception ex)
                            {
                                ErrorLogger.LogWarning(-1, nameof(FindMismatchDetails), $"Error adding missing Microsoft row: {ex.Message}", string.Empty, string.Empty, string.Empty);
                            }
                        }
                    }
                }

                foreach (var sixdotOneGroup in groupedSixdotOneData)
                {
                    if (!sixdotOneGroup.Rows.Any())
                        continue;
                    string customerKey = sixdotOneGroup.Key;
                    string[] keyParts = customerKey.Split(" - ");
                    string customerName = keyParts.Length > 0 ? keyParts[0] : string.Empty;
                    string customerDomain = keyParts.Length > 1 ? keyParts[1] : string.Empty;

                    var microsoftGroup = groupedMicrosoftData.FirstOrDefault(g =>
                        g.Key.Contains(customerName, StringComparison.OrdinalIgnoreCase) ||
                        g.Key.Contains(customerDomain, StringComparison.OrdinalIgnoreCase));

                    if (microsoftGroup == null)
                    {
                        foreach (var sixdotOneRow in sixdotOneGroup.Rows)
                        {
                            try
                            {
                                DataRow newRow = mismatchTable.NewRow();
                                newRow["Source"] = "MSP Hub";
                                foreach (var column in columnsMspHubBilling)
                                    newRow[column] = sixdotOneRow[column];
                                newRow["Reason"] = "This Line Item is missing in Microsoft Invoice.";
                                mismatchTable.Rows.Add(newRow);
                            }
                            catch (Exception ex)
                            {
                                ErrorLogger.LogWarning(-1, nameof(FindMismatchDetails), $"Error adding missing MSP Hub row: {ex.Message}", string.Empty, string.Empty, string.Empty);
                            }
                        }
                    }
                }

                ProcessMismatchSummary(mismatchTable, groupedMicrosoftData, groupedSixdotOneData, stopwatch);
                return mismatchTable;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError(-1, nameof(FindMismatchDetails), $"Critical error in FindMismatchDetails: {ex.Message}", string.Empty, string.Empty, string.Empty);
                return new DataTable();
            }
        }

        private static decimal CalculatePercentage(decimal value, decimal percent)
        {
            return (value * percent) / 100;
        }

        private static string SafeGetString(DataRow row, string columnName)
        {
            if (row == null || string.IsNullOrWhiteSpace(columnName) || !row.Table.Columns.Contains(columnName))
                return string.Empty;
            var val = row[columnName];
            return val == null || val == DBNull.Value ? string.Empty : val.ToString();
        }

        private static decimal SafeConvertToDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;
            return Convert.ToDecimal(value);
        }

        private static void LogComparisonStart()
        {
            ErrorLogger.LogWarning(-1, "INFO", "******************** Comparison of Microsoft and MSP Hub Invoice Run ********************", string.Empty, string.Empty, string.Empty);
            ErrorLogger.LogWarning(-1, "INFO", $"Comparison of Microsoft and MSP Hub Invoice started at: {DateTime.Now}", string.Empty, string.Empty, string.Empty);
        }

        private static void ProcessMismatchSummary(DataTable mismatchTable, List<GroupedCustomerData> groupedMicrosoftData, List<GroupedCustomerData> groupedSixdotOneData, Stopwatch stopwatch)
        {
            int microsoftRowCount = mismatchTable.AsEnumerable().Count(row => row["Source"].ToString() == "Microsoft" && row.ItemArray.All(value => value.ToString() != "-----"));
            int sixdotOneRowCount = mismatchTable.AsEnumerable().Count(row => row["Source"].ToString() == "MSP Hub" && row.ItemArray.All(value => value.ToString() != "-----"));

            ErrorLogger.LogWarning(-1, "INFO", $"Total Line Items Processed (Microsoft): {microsoftRowCount}", string.Empty, string.Empty, string.Empty);
            ErrorLogger.LogWarning(-1, "INFO", $"Total Line Items Processed (MSP Hub): {sixdotOneRowCount}", string.Empty, string.Empty, string.Empty);

            int uniqueMicrosoftCustomers = groupedMicrosoftData.Select(g => g.Key).Distinct().Count();
            int uniqueSixdotOneCustomers = groupedSixdotOneData.Select(g => g.Key).Distinct().Count();

            ErrorLogger.LogWarning(-1, "INFO", $"Total Customers Processed for Mismatch (Microsoft): {uniqueMicrosoftCustomers}", string.Empty, string.Empty, string.Empty);
            ErrorLogger.LogWarning(-1, "INFO", $"Total Customers Processed for Mismatch (MSP Hub): {uniqueSixdotOneCustomers}", string.Empty, string.Empty, string.Empty);

            stopwatch.Stop();
            DateTime endTime = DateTime.Now;
            ErrorLogger.LogWarning(-1, "INFO", $"Comparison of Microsoft and MSP Hub Invoice ended at: {endTime}", string.Empty, string.Empty, string.Empty);

            TimeSpan elapsed = stopwatch.Elapsed;
            string formattedElapsed = elapsed.TotalSeconds < 1 ? $"{elapsed.TotalMilliseconds:F2} ms" : elapsed.ToString(@"hh\:mm\:ss");

            ErrorLogger.LogWarning(-1, "INFO", $"Total time taken: {formattedElapsed}", string.Empty, string.Empty, string.Empty);
        }
    }

    public class GroupedCustomerData
    {
        public string Key { get; set; } = string.Empty;
        public List<DataRow> Rows { get; set; } = new();
    }
}
