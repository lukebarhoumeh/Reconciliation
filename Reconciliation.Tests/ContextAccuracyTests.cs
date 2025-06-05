using Reconciliation;
using System.Data;
using System.Linq;

namespace Reconciliation.Tests
{
    public class ContextAccuracyTests
    {
        [Fact]
        public void ContextShowsMissingValues()
        {
            var dt = new DataTable();
            dt.Columns.Add("CustomerName");
            dt.Columns.Add("SkuId");
            dt.Columns.Add("OrderDate");
            dt.Columns.Add("Quantity");
            dt.Rows.Add("", "", "bad", "1");

            ErrorLogger.Clear();
            DataQualityValidator.Run(dt, "file.csv");

            var entry = ErrorLogger.Entries.First();
            Assert.Contains("Customer: (missing)", entry.Context);
            Assert.Contains("SKU: (missing)", entry.Context);
        }
    }
}
