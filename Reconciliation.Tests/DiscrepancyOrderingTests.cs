using Reconciliation;
using System.Data;

namespace Reconciliation.Tests
{
    public class DiscrepancyOrderingTests
    {
        [Fact]
        public void GetMismatches_ReturnsSortedRows()
        {
            var left = new DataTable();
            left.Columns.Add("A");
            left.Columns.Add("B");
            left.Rows.Add("1", "x");

            var right = new DataTable();
            right.Columns.Add("A");
            right.Columns.Add("B");
            right.Rows.Add("2", "y");

            var detector = new DiscrepancyDetector();
            detector.Compare(left, right);
            var table = detector.GetMismatches();

            Assert.Equal("A", table.Rows[0]["Field Name"]);
            Assert.Equal("B", table.Rows[1]["Field Name"]);
        }
    }
}
