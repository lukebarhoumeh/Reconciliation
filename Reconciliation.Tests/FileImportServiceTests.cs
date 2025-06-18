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
            var service = new FileImportService(false);
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
            var service = new FileImportService(true);
            ErrorLogger.Clear();
            var view = service.ImportSixDotOneInvoice(path);
            Assert.Single(view);
            DataRow row = view.Table.Rows[0];
            Assert.Equal("2", row["SkuId"]);
            Assert.True(view.Table.Columns.Contains("BillingCycle"));
            Assert.False(view.Table.Columns.Contains("BillingFrequency"));
        }

        [Fact]
        public void ImportSixDotOneInvoice_FuzzySkuNameMapped()
        {
            var path = Path.Combine("TestData", "sixdotone_skuname.csv");
            var service = new FileImportService(true);
            ErrorLogger.Clear();
            var view = service.ImportSixDotOneInvoice(path);
            Assert.Single(view);
            Assert.Contains("SkuId", view.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        }
    }
}
