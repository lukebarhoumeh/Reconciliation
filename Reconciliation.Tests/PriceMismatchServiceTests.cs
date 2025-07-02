using System.Data;
using System.IO;
using OfficeOpenXml;
using Reconciliation;

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
            hub.Columns.Add("Quantity", typeof(decimal));
            hub.Columns.Add("Subtotal", typeof(decimal));
            hub.Rows.Add("a.com","p1",1m,10m);

            var ms = hub.Clone();
            ms.Rows.Add("a.com","p1",1m,10m);
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

        [Fact]
        public void GetPriceMismatches_OffsetsCreditAndDebit()
        {
            var svc = new PriceMismatchService();
            var hub = new DataTable();
            hub.Columns.Add("CustomerDomainName");
            hub.Columns.Add("ProductId");
            hub.Columns.Add("ChargeType");
            hub.Columns.Add("Quantity", typeof(decimal));
            hub.Columns.Add("Subtotal", typeof(decimal));
            // Hub records only the net additional license
            hub.Rows.Add("a.com", "p1", "Usage", 1m, 1m);

            var ms = hub.Clone();
            // Microsoft invoice credits original charge then debits new amount
            ms.Rows.Add("a.com", "p1", "Credit", -100m, 0m);
            ms.Rows.Add("a.com", "p1", "Usage", 101m, 101m);

            var result = svc.GetPriceMismatches(hub, ms);
            Assert.Empty(result.Rows);
        }
    }
}
