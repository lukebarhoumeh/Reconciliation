using Reconciliation;
using System.Data;
using System.IO;

namespace Reconciliation.Tests
{
    public class CsvNormalizerFileTests
    {
        [Fact]
        public void NormalizeCsv_ParsesFileCorrectly()
        {
            var file = Path.Combine("TestData", "sample.csv");
            ErrorLogger.Clear();
            var view = CsvNormalizer.NormalizeCsv(file);
            DataTable table = view.Table;
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal("001", table.Rows[0]["Id"]);
            Assert.Equal("2", table.Rows[0]["Quantity"]);
            Assert.Equal("2024-01-02", table.Rows[0]["Date"]);
            Assert.Contains(ErrorLogger.Entries, e => e.RowNumber == 2 && e.ColumnName == "Quantity");
        }
    }
}
