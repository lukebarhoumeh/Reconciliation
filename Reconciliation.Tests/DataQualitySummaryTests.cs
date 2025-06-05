using Reconciliation;
using System.Data;

namespace Reconciliation.Tests
{
    public class DataQualitySummaryTests
    {
        [Fact]
        public void GetSummary_ReturnsCounts()
        {
            var dt = new DataTable();
            dt.Columns.Add("Quantity");
            dt.Columns.Add("PartnerDiscountPercentage");
            dt.Columns.Add("CustomerSubTotal");

            dt.Rows.Add("1", "200", "");
            dt.Rows.Add("1", "50", "10");

            ErrorLogger.Clear();
            DataQualityValidator.Run(dt, "file.csv");
            string summary = DataQualityValidator.GetSummary();
            Assert.Contains("Total rows: 2", summary);
            Assert.Contains("Errors: 1", summary);
            Assert.Contains("Warnings", summary);
            Assert.Contains("CustomerSubTotal", summary);
        }
    }
}
