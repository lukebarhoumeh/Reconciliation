using Reconciliation;
using System.Data;

namespace Reconciliation.Tests
{
    public class DiscrepancySummaryTests
    {
        [Fact]
        public void GetSummary_ReportsDiscrepancies()
        {
            var left = new DataTable();
            left.Columns.Add("Amount");
            left.Rows.Add("10");

            var right = new DataTable();
            right.Columns.Add("Amount");
            right.Rows.Add("11");

            var detector = new DiscrepancyDetector();
            detector.Compare(left, right);
            string summary = detector.GetSummary();
            Assert.Contains("Discrepancies found: 1", summary);
            Assert.Contains("Numeric mismatch", summary);
        }
    }
}
