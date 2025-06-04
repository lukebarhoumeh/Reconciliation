using Reconciliation;
using System.Data;

namespace Reconciliation.Tests
{
    public class CsvNormalizerTests
    {
        [Fact]
        public void ErrorLoggerCollectsMessages()
        {
            ErrorLogger.Clear();
            ErrorLogger.LogError(1, "Col", "msg", "val", "file.csv", "ctx");
            Assert.Single(ErrorLogger.Entries);
        }

        [Fact]
        public void BlankNumericField_DoesNotLogError()
        {
            var table = new DataTable();
            table.Columns.Add("Quantity");
            table.Rows.Add("");
            ErrorLogger.Clear();
            CsvNormalizer.NormalizeDataTable(table);
            Assert.Empty(ErrorLogger.Entries);
        }

        [Fact]
        public void InvalidNumericField_LogsErrorWithRowInfo()
        {
            var table = new DataTable();
            table.Columns.Add("Quantity");
            table.Rows.Add("abc");
            ErrorLogger.Clear();
            CsvNormalizer.NormalizeDataTable(table);
            Assert.Single(ErrorLogger.Entries);
            Assert.Contains("Quantity", ErrorLogger.Entries[0].ColumnName);
        }

        [Fact]
        public void SummaryCountsRepeatedErrors()
        {
            ErrorLogger.Clear();
            ErrorLogger.LogError(1, "col", "sample", "", "f.csv", "");
            ErrorLogger.LogError(2, "col", "sample", "", "f.csv", "");
            Assert.True(ErrorLogger.ErrorSummary.TryGetValue("sample", out var count));
            Assert.Equal(2, count);
        }
    }
}
