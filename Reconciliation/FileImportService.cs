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

        public FileImportService()
        {
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
            DataQualityValidator.Run(dataView.Table, fileInfo.Name);
            SchemaValidator.RequireColumns(dataView.Table, "MSP Hub invoice", _requiredMspHubColumns);

            if (dataView.Table.Columns.Contains("SkuId"))
            {
                foreach (DataRowView rowView in dataView)
                {
                    string skuId = SafeGetString(rowView.Row, "SkuId");
                    rowView["SkuId"] = skuId.TrimStart('0');
                }
            }

            if (dataView.Table.Columns.Contains("BillingFrequency"))
                dataView.Table.Columns["BillingFrequency"].ColumnName = "BillingCycle";
            if (dataView.Table.Columns.Contains("ResourceName"))
                dataView.Table.Columns["ResourceName"].ColumnName = "ProductName";

            for (int i = dataView.Count - 1; i >= 0; i--)
            {
                var row = dataView[i].Row;
                if (SafeGetString(row, "ProductName") == "Azure plan")
                {
                    dataView.Delete(i);
                }
            }

            if (dataView.Table.Columns.Contains("ValidFrom"))
                dataView.Table.Columns["ValidFrom"].ColumnName = "ChargeStartDate";
            if (dataView.Table.Columns.Contains("ValidTo"))
                dataView.Table.Columns["ValidTo"].ColumnName = "ChargeEndDate";
            if (dataView.Table.Columns.Contains("PurchaseDate"))
                dataView.Table.Columns["PurchaseDate"].ColumnName = "OrderDate";
            if (dataView.Table.Columns.Contains("PartnerTotal"))
                dataView.Table.Columns["PartnerTotal"].ColumnName = "Total";

            dataView = ReorderColumns(dataView.Table, _uniqueKeyColumns);
            return dataView;
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
