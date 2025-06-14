using System.Data;
using System.IO;
using OfficeOpenXml;

namespace Reconciliation.Tests
{
    public class PriceMismatchServiceTests
    {
        [Fact]
        public void GetPriceMismatches_NoDifferences_ReturnsEmpty()
        {
            var svc = new PriceMismatchService();
            var hub = new DataTable();
            hub.Columns.Add("CustomerDomainName");
            hub.Columns.Add("ProductId");
            hub.Columns.Add("SkuId");
            hub.Columns.Add("ChargeType");
            hub.Columns.Add("Term");
            hub.Columns.Add("BillingCycle");
            hub.Columns.Add("EffectiveUnitPrice", typeof(decimal));
            hub.Columns.Add("Quantity", typeof(decimal));
            hub.Columns.Add("ProductName");
            hub.Rows.Add("a.com","p1","1","Usage","T","M",1m,1m,"X");

            var ms = hub.Clone();
            ms.Columns.Add("SubscriptionDescription");
            ms.Rows.Add("a.com","p1","1","Usage","T","M",1m,1m,"X","X");
            var result = svc.GetPriceMismatches(hub, ms);
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void ExportPriceMismatchesToExcel_WritesFile()
        {
            var svc = new PriceMismatchService();
            var table = new DataTable();
            table.Columns.Add("SkuId");
            table.Columns.Add("PriceInMicrosoft", typeof(decimal));
            table.Columns.Add("PriceDifference", typeof(decimal));
            table.Rows.Add("1", 10m, 1m);
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".xlsx");
            svc.ExportPriceMismatchesToExcel(table, path);
            using var package = new ExcelPackage(new FileInfo(path));
            Assert.Equal("SkuId", package.Workbook.Worksheets[0].Cells[1, 1].Text);
        }
    }
}
