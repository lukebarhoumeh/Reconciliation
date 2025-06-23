using Reconciliation;
using System.Data;
using System.IO;

namespace Reconciliation.Tests
{
    public class FileImportServiceTests
    {
        [Fact]
        public void ImportMicrosoftInvoice_FiltersAndSplits()
        {
            var path = Path.Combine("TestData", "microsoft.csv");
            var service = new FileImportService();
            ErrorLogger.Clear();
            var view = service.ImportMicrosoftInvoice(path);
            Assert.Single(view);
            DataRow row = view.Table.Rows[0];
            Assert.Equal("Annual", row["Term"]);
            Assert.Equal("Monthly", row["BillingCycle"]);
        }

        [Fact]
        public void ImportSixDotOneInvoice_Normalizes()
        {
            var path = Path.Combine("TestData", "sixdotone.csv");
            var service = new FileImportService();
            ErrorLogger.Clear();
            var view = service.ImportSixDotOneInvoice(path);
            Assert.Single(view);
            DataRow row = view.Table.Rows[0];
            Assert.Equal("2", row["SkuId"]);
            Assert.True(view.Table.Columns.Contains("BillingCycle"));
            Assert.True(view.Table.Columns.Contains("BillingFrequency"));
        }

        [Fact]
        public void ImportSixDotOneInvoice_MapsColumns()
        {
            var path = Path.Combine("TestData", "msphub_raw.csv");
            var service = new FileImportService();
            ErrorLogger.Clear();
            var view = service.ImportSixDotOneInvoice(path);
            Assert.Single(view);
            DataRow row = view.Table.Rows[0];
            Assert.Equal("PN-1", row["SkuId"]); // PartNumber preferred over SkuName
            Assert.Equal("INV1", row["InvoiceNumber"]);
            Assert.False(view.Table.Columns.Contains("ExtraColumn"));
        }

    }
}
