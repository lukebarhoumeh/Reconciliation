using System;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;

namespace Reconciliation
{
    /// <summary>
    /// Validates MSP Hub invoice records for business rule compliance.
    /// </summary>
    public record InvoiceValidationResult(DataTable InvalidRows, int HighPriority, int LowPriority);

    public class InvoiceValidationService
    {
        /// <summary>
        /// Validates the given invoice table and returns only invalid rows with
        /// validation error details appended as columns.
        /// </summary>
        /// <param name="msphub">Invoice data to validate.</param>
        /// <returns>Result containing invalid rows and error counts.</returns>
        public InvoiceValidationResult ValidateInvoice(DataTable msphub)
        {
            if (msphub == null)
                throw new ArgumentNullException(nameof(msphub));

            var stopwatch = Stopwatch.StartNew();
            EnsureValidationColumns(msphub);
            var rowsToRemove = new List<DataRow>();
            int high = 0, low = 0;

            foreach (DataRow row in msphub.Rows)
            {
                bool invalid = ValidateRow(msphub, row, ref high, ref low);
                if (!invalid)
                    rowsToRemove.Add(row);
            }

            RemoveValidRows(msphub, rowsToRemove);
            stopwatch.Stop();
            return new InvoiceValidationResult(msphub, high, low);
        }

        private static void EnsureValidationColumns(DataTable table)
        {
            string[] cols =
            {
                "Eff. Days Validation",
                "Partner Discount Validation",
                "Discount Hierarchy Check",
                "Pricing Consistency Check"
            };
            foreach (var col in cols)
            {
                if (!table.Columns.Contains(col))
                    table.Columns.Add(col, typeof(string));
            }
        }

        private static bool ValidateRow(DataTable table, DataRow row,
            ref int highPriorityErrors, ref int lowPriorityErrors)
        {
            string startDateColumn = table.Columns.Contains("ChargeStartDate")
                ? "ChargeStartDate" : "ValidFrom";
            string endDateColumn = table.Columns.Contains("ChargeEndDate")
                ? "ChargeEndDate" : "ValidTo";
            string msrpColumn = table.Columns.Contains("MSRP")
                ? "MSRP" : "MSRPPrice";

            DateTime validFrom = DateTime.TryParse(SafeGetString(row, startDateColumn), out var tmpFrom)
                ? tmpFrom : DateTime.MinValue;
            DateTime validTo = DateTime.TryParse(SafeGetString(row, endDateColumn), out var tmpTo)
                ? tmpTo : DateTime.MinValue;

            int effectiveDays = int.TryParse(SafeGetString(row, "EffectiveDays"), out var ed) ? ed : 0;
            int calculatedDays = (validTo.Date - validFrom.Date).Days + 1;

            decimal msrpPrice = SafeConvertToDecimal(SafeGetString(row, msrpColumn));
            decimal partnerDiscountPercentage = SafeConvertToDecimal(SafeGetString(row, "PartnerDiscountPercentage"));
            decimal customerDiscountPercentage = SafeConvertToDecimal(SafeGetString(row, "CustomerDiscountPercentage"));
            decimal partnerTotal = SafeConvertToDecimal(SafeGetString(row, "Total"));
            decimal customerSubtotal = SafeConvertToDecimal(SafeGetString(row, "CustomerSubtotal"));

            bool isInvalid = false;

            if (calculatedDays != effectiveDays)
            {
                row["Eff. Days Validation"] = $"Mismatch: Expected EffectiveDays is {calculatedDays} days, but found {effectiveDays} days.";
                isInvalid = true;
                highPriorityErrors++;
            }

            if (msrpPrice > 5)
            {
                var rounded = Math.Round(partnerDiscountPercentage, 1,
                    MidpointRounding.AwayFromZero);
                const decimal minDiscount = 18.0m;
                const decimal maxDiscount = 20.1m; // inclusive upper bound
                if (partnerDiscountPercentage <= 0)
                {
                    row["Partner Discount Validation"] = $"Invalid: MSRP ({NumericFormatter.Money(msrpPrice)}) is greater than 5, but PartnerDiscountPercentage is {NumericFormatter.Percent(partnerDiscountPercentage)}%.";
                    isInvalid = true;
                    highPriorityErrors++;
                }
                else
                {
                    if (rounded < minDiscount || rounded > maxDiscount)
                    {
                        row["Partner Discount Validation"] = $"Invalid: PartnerDiscountPercentage ({NumericFormatter.Percent(partnerDiscountPercentage)}%) is not between {NumericFormatter.Percent(minDiscount)}% and {NumericFormatter.Percent(maxDiscount)}%.";
                        isInvalid = true;
                        highPriorityErrors++;
                    }
                }
            }

            int rowNumber = table.Rows.IndexOf(row) + 1;
            if (customerDiscountPercentage >= 100)
            {
                ErrorLogger.LogWarning(rowNumber, nameof(customerDiscountPercentage),
                    "Customer at 100% discount – hierarchy check skipped.",
                    customerDiscountPercentage.ToString(), "MSPHubInvoice", string.Empty);
            }
            else if (partnerDiscountPercentage < customerDiscountPercentage)
            {
                row["Discount Hierarchy Check"] = $"Invalid: PartnerDiscountPercentage ({NumericFormatter.Percent(partnerDiscountPercentage)}%) is less than CustomerDiscountPercentage ({NumericFormatter.Percent(customerDiscountPercentage)}%).";
                isInvalid = true;
                lowPriorityErrors++;
            }

            if (customerSubtotal == 0 && customerDiscountPercentage >= 100)
            {
                ErrorLogger.LogWarning(rowNumber, nameof(customerSubtotal),
                    "Customer billed $0 due to 100% discount – consistency check skipped.",
                    customerSubtotal.ToString(), "MSPHubInvoice", string.Empty);
            }
            else if (Math.Abs(partnerTotal) > Math.Abs(customerSubtotal))
            {
                row["Pricing Consistency Check"] = $"Invalid: PartnerTotal ({NumericFormatter.Money(partnerTotal)}) is greater than CustomerSubTotal ({NumericFormatter.Money(customerSubtotal)}).";
                isInvalid = true;
                lowPriorityErrors++;
            }

            return isInvalid;
        }

        private static void RemoveValidRows(DataTable table, List<DataRow> rows)
        {
            foreach (var row in rows)
                table.Rows.Remove(row);
        }

        internal static decimal SafeConvertToDecimal(object? value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            return decimal.TryParse(Convert.ToString(value), out var d) ? d : 0m;
        }

        internal static string SafeGetString(DataRow row, string columnName)
        {
            if (row == null || string.IsNullOrWhiteSpace(columnName) || !row.Table.Columns.Contains(columnName))
                return string.Empty;
            var val = row[columnName];
            return val == null || val == DBNull.Value ? string.Empty : val.ToString();
        }

        internal static string FormatElapsedTime(TimeSpan elapsed)
        {
            return elapsed.TotalSeconds < 1
                ? $"{elapsed.TotalMilliseconds:F2} ms"
                : elapsed.ToString(@"hh\:mm\:ss");
        }
    }
}
