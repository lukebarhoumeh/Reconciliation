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
                Assert.Equal(2, lines.Length);
                }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Warns_When_CriticalField_ExceedsThreshold()
        {
            var dt = new DataTable();
            dt.Columns.Add("CustomerSubTotal");
            dt.Columns.Add("Quantity");
            for (int i = 0; i < 10; i++)
            {
                var val = i < 2 ? "0" : "1";
                dt.Rows.Add(val, "1");
            }

            ErrorLogger.Clear();
            DataQualityValidator.Run(dt, "test.csv");
            Assert.Contains(ErrorLogger.Entries,
                e => e.ColumnName == "CustomerSubTotal");
        }
    }
}
