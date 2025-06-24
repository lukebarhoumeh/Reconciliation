using System.Data;
using System.IO;
using System.Linq;

namespace Reconciliation
{
    /// <summary>
    /// Provides helper methods to import and normalize invoice CSV files.
    /// </summary>
    public class FileImportService
    {
        private readonly string[] _uniqueKeyColumns =
        {
            "CustomerDomainName", "ProductId", "SkuId", "ChargeType", "Term", "BillingCycle"
        };
        private readonly string[] _requiredMicrosoftColumns =
        {
            "CustomerDomainName", "ProductId", "SkuId", "ChargeType", "TermAndBillingCycle"
        };
        private readonly string[] _requiredMspHubColumns =
        {
            "InternalReferenceId", "SkuId", "BillingCycle"
        };

        // Canonical Microsoft column order used for partner imports
        private static readonly string[] _microsoftColumns =
        {
            "PartnerId","CustomerId","CustomerName","CustomerDomainName","CustomerCountry",
            "InvoiceNumber","MpnId","Tier2MpnId","OrderId","OrderDate","ProductId","SkuId",
            "AvailabilityId","SkuName","ProductName","ChargeType","UnitPrice","Quantity",
            "Subtotal","TaxTotal","Total","Currency","PriceAdjustmentDescription",
            "PublisherName","PublisherId","SubscriptionDescription","SubscriptionId",
            "ChargeStartDate","ChargeEndDate","TermAndBillingCycle","EffectiveUnitPrice",
            "UnitType","AlternateId","BillableQuantity","BillingFrequency","PricingCurrency",
            // Additional split columns used as comparison keys
            "Term","BillingCycle"
        };

        public FileImportService()
        {
        }

        public DataTable Import(string filePath)
        {
            var dataView = CsvNormalizer.NormalizeCsv(filePath);
            var type = SourceTypeDetector.FromFilename(filePath);
            var normalised = CsvSchemaMapper.Normalize(dataView.Table, type);
            DataQualityValidator.Run(normalised, Path.GetFileName(filePath));
            if (type == SourceType.Microsoft)
                SchemaValidator.RequireColumns(normalised, "Microsoft invoice", _requiredMicrosoftColumns);
            else
                SchemaValidator.RequireColumns(normalised, "Partner invoice", _requiredMspHubColumns);
            return normalised;
        }

        /// <summary>
        /// Import a Microsoft invoice CSV file.
        /// </summary>
        /// <param name="filePath">Path to the CSV file.</param>
        /// <returns>Normalized DataView.</returns>
        public DataView ImportMicrosoftInvoice(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var dataView = CsvNormalizer.NormalizeCsv(fileInfo.FullName);
            if (dataView.Table.Rows.Count == 0)
            {
                ErrorLogger.LogError(-1, "-", "File is empty", string.Empty, fileInfo.Name, string.Empty);
                throw new ArgumentException("The selected file contains no rows.");
            }
            DataQualityValidator.Run(dataView.Table, fileInfo.Name);
            SchemaValidator.RequireColumns(dataView.Table, "Microsoft invoice", _requiredMicrosoftColumns);

            for (int i = dataView.Count - 1; i >= 0; i--)
            {
                var row = dataView[i].Row;
                if (SafeGetString(row, "SubscriptionDescription") == "Azure plan")
                {
                    dataView.Delete(i);
                }
            }

            if (!dataView.Table.Columns.Contains("Term") || !dataView.Table.Columns.Contains("BillingCycle"))
            {
                SplitColumn(dataView.Table, "TermAndBillingCycle", "Term", "BillingCycle");
            }

            dataView = ReorderColumns(dataView.Table, _uniqueKeyColumns.Concat(new[] { "TermAndBillingCycle", "BillingFrequency" }).ToArray());
            return dataView;
        }

        /// <summary>
        /// Import an MSP Hub invoice CSV file.
        /// </summary>
        /// <param name="filePath">Path to the CSV file.</param>
        /// <returns>Normalized DataView.</returns>
        public DataView ImportSixDotOneInvoice(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var dataView = CsvNormalizer.NormalizeCsv(fileInfo.FullName);
            if (dataView.Table.Rows.Count == 0)
            {
                ErrorLogger.LogError(-1, "-", "File is empty", string.Empty, fileInfo.Name, string.Empty);
                throw new ArgumentException("The selected file contains no rows.");
            }

            // Map all columns to Microsoft equivalents before any validation
            var mapped = MapMspHubColumns(dataView.Table);
            DataQualityValidator.Run(mapped, fileInfo.Name);
            SchemaValidator.RequireColumns(mapped, "MSP Hub invoice", _requiredMspHubColumns);

            var view = mapped.DefaultView;

            if (view.Table.Columns.Contains("SkuId"))
            {
                foreach (DataRowView rowView in view)
                {
                    string skuId = SafeGetString(rowView.Row, "SkuId");
                    rowView["SkuId"] = skuId.TrimStart('0');
                }
            }

            if (view.Table.Columns.Contains("ResourceName"))
                view.Table.Columns["ResourceName"].ColumnName = "ProductName";

            for (int i = view.Count - 1; i >= 0; i--)
            {
                var row = view[i].Row;
                if (SafeGetString(row, "ProductName") == "Azure plan")
                {
                    view.Delete(i);
                }
            }

            if (view.Table.Columns.Contains("ValidFrom"))
                view.Table.Columns["ValidFrom"].ColumnName = "ChargeStartDate";
            if (view.Table.Columns.Contains("ValidTo"))
                view.Table.Columns["ValidTo"].ColumnName = "ChargeEndDate";
            if (view.Table.Columns.Contains("PurchaseDate"))
                view.Table.Columns["PurchaseDate"].ColumnName = "OrderDate";
            if (view.Table.Columns.Contains("PartnerTotal"))
                view.Table.Columns["PartnerTotal"].ColumnName = "Total";

            view = ReorderColumns(view.Table, _uniqueKeyColumns);
            return view;
        }

        /// <summary>
        /// Map MSP Hub columns to the Microsoft invoice schema.
        /// Extra columns are discarded and missing ones are added empty so the
        /// returned table exactly matches the Microsoft column order.
        /// </summary>
        private static DataTable MapMspHubColumns(DataTable table)
        {
            DataTable mapped = new();
            foreach (string col in _microsoftColumns)
                mapped.Columns.Add(col, typeof(string));

            foreach (DataRow row in table.Rows)
            {
                DataRow newRow = mapped.NewRow();
                foreach (string col in _microsoftColumns)
                {
                    newRow[col] = col switch
                    {
                        "PartnerId" => SafeGetString(row, "PartnerId"),
                        "CustomerId" => SafeGetString(row, "CustomerId"),
                        "CustomerName" => SafeGetString(row, "CustomerName"),
                        "CustomerDomainName" => SafeGetString(row, "CustomerDomainName"),
                        "CustomerCountry" => SafeGetString(row, "CustomerCountry"),
                        "InvoiceNumber" => SafeGetString(row, "PartnerInvoiceNumber"),
                        "MpnId" => string.Empty,
                        "Tier2MpnId" => string.Empty,
                        "OrderId" => SafeGetString(row, "OrderNumber"),
                        "OrderDate" => SafeGetString(row, "OrderDate"),
                        "ProductId" => SafeGetString(row, "ProductId"),
                        "SkuId" => !string.IsNullOrEmpty(SafeGetString(row, "PartNumber"))
                                        ? SafeGetString(row, "PartNumber")
                                        : !string.IsNullOrEmpty(SafeGetString(row, "SkuId"))
                                            ? SafeGetString(row, "SkuId")
                                            : SafeGetString(row, "SkuName"),
                        "AvailabilityId" => string.Empty,
                        "SkuName" => SafeGetString(row, "SkuName"),
                        "ProductName" =>
                            !string.IsNullOrEmpty(SafeGetString(row, "ProductName"))
                                ? SafeGetString(row, "ProductName")
                                : SafeGetString(row, "ResourceName"),
                        "ChargeType" => SafeGetString(row, "ChargeType"),
                        "UnitPrice" => SafeGetString(row, "PartnerUnitPrice"),
                        "Quantity" => SafeGetString(row, "Quantity"),
                        "Subtotal" => SafeGetString(row, "PartnerSubTotal"),
                        "TaxTotal" => SafeGetString(row, "PartnerTaxTotal"),
                        "Total" => SafeGetString(row, "PartnerTotal"),
                        "Currency" => SafeGetString(row, "PricingCurrency"),
                        "PriceAdjustmentDescription" => string.Empty,
                        "PublisherName" => string.Empty,
                        "PublisherId" => string.Empty,
                        "SubscriptionDescription" => string.Empty,
                        "SubscriptionId" => SafeGetString(row, "SubscriptionId"),
                        "ChargeStartDate" => SafeGetString(row, "ChargeStartDate"),
                        "ChargeEndDate" => SafeGetString(row, "ChargeEndDate"),
                        "TermAndBillingCycle" =>
                            string.IsNullOrEmpty(SafeGetString(row, "BillingCycle"))
                                ? SafeGetString(row, "Term")
                                : $"{SafeGetString(row, "Term")} {SafeGetString(row, "BillingCycle")}",
                        "EffectiveUnitPrice" => SafeGetString(row, "PartnerEffectiveUnitPrice"),
                        "UnitType" => string.Empty,
                        "AlternateId" => string.Empty,
                        "BillableQuantity" => string.Empty,
                        "BillingFrequency" => SafeGetString(row, "BillingCycle"),
                        "PricingCurrency" => SafeGetString(row, "PricingCurrency"),
                        "Term" => SafeGetString(row, "Term"),
                        "BillingCycle" => SafeGetString(row, "BillingCycle"),
                        _ => string.Empty
                    };
                }
                mapped.Rows.Add(newRow);
            }

            return mapped;
        }

        private static DataView ReorderColumns(DataTable table, string[] columnOrder)
        {
            DataTable newTable = new();
            foreach (string columnName in columnOrder)
            {
                if (table.Columns.Contains(columnName))
                    newTable.Columns.Add(columnName, table.Columns[columnName].DataType);
            }
            foreach (DataColumn column in table.Columns)
            {
                if (!newTable.Columns.Contains(column.ColumnName))
                    newTable.Columns.Add(column.ColumnName, column.DataType);
            }
            foreach (DataRow row in table.Rows)
            {
                DataRow newRow = newTable.Rows.Add();
                foreach (DataColumn column in newTable.Columns)
                    newRow[column.ColumnName] = row[column.ColumnName];
            }
            return newTable.DefaultView;
        }

        private static void SplitColumn(DataTable dataTable, string sourceColumnName, string termColumnName, string billingCycleColumnName)
        {
            dataTable.Columns.Add(termColumnName, typeof(string));
            dataTable.Columns.Add(billingCycleColumnName, typeof(string));
            foreach (DataRow row in dataTable.Rows)
            {
                string? termAndBillingCycle = row[sourceColumnName]?.ToString();
                string billingFrequency = row["BillingFrequency"]?.ToString() ?? string.Empty;
                DetermineTermAndBillingCycle(termAndBillingCycle, billingFrequency, out string term, out string billingCycle);
                row[termColumnName] = term;
                row[billingCycleColumnName] = billingCycle;
            }
        }

        private static void DetermineTermAndBillingCycle(string? termAndBillingCycle, string billingFrequency, out string term, out string billingCycle)
        {
            term = "NA";
            billingCycle = "NA";
            switch (termAndBillingCycle)
            {
                case "One-Year commitment for monthly/yearly billing":
                    term = "Annual";
                    billingCycle = string.IsNullOrEmpty(billingFrequency) ? "Annual" : "Monthly";
                    break;
                case "One-Month commitment for monthly billing":
                    term = "Monthly";
                    billingCycle = "Monthly";
                    break;
                case "Three-3 Years commitment for monthly/3 Years/yearly billing":
                    term = "Triennial";
                    billingCycle = "Monthly";
                    break;
                case "1 Cache Instance Hour":
                    term = "Monthly";
                    billingCycle = "Monthly";
                    break;
                case "One-Year commitment for yearly billing":
                    term = "Annual";
                    billingCycle = "Annual";
                    break;
                case null or "":
                    term = "None";
                    billingCycle = "OneTime";
                    break;
            }
        }

        private static string SafeGetString(DataRow row, string columnName)
        {
            if (row == null || string.IsNullOrWhiteSpace(columnName) || !row.Table.Columns.Contains(columnName))
                return string.Empty;
            var val = row[columnName];
            return val == null || val == DBNull.Value ? string.Empty : val.ToString();
        }
    }
}
