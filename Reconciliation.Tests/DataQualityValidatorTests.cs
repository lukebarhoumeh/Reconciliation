using Reconciliation;
using System.Data;
using System.IO;
using System.Linq;
namespace Reconciliation.Tests
{
    public class DataQualityValidatorTests
    {
        [Fact]
        public void LogsZeroOrBlankFields()
        {
            var dt = new DataTable();
            dt.Columns.Add("Quantity");
            dt.Columns.Add("PartnerDiscountPercentage");
            dt.Rows.Add("5", "0");
            ErrorLogger.Clear();
            DataQualityValidator.Run(dt, "test.csv");
            Assert.Contains(ErrorLogger.Entries, e => e.ColumnName == "PartnerDiscountPercentage" && e.RowNumber == 1);
        }

        [Fact]
        public void Export_WritesStructuredCsv()
        {
            ErrorLogger.Clear();
            ErrorLogger.LogError(1, "Col", "Problem", "123", "file.csv", "ctx");
            var path = Path.GetTempFileName();
            try
            {
                ErrorLogger.Export(path);
                var lines = File.ReadAllLines(path);
                Assert.Contains("Timestamp,ErrorLevel", lines[0]);
                Assert.Contains("Total Errors", lines[^1]);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
