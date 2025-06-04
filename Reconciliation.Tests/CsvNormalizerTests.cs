using Xunit;
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
            ErrorLogger.LogError("test message");
            Assert.Single(ErrorLogger.Errors);
        }

        [Fact]
        public void BlankNumericField_DoesNotLogError()
        {
            var table = new DataTable();
            table.Columns.Add("Quantity");
            table.Rows.Add("");
            ErrorLogger.Clear();
            CsvNormalizer.NormalizeDataTable(table);
            Assert.Empty(ErrorLogger.Errors);
        }

        [Fact]
        public void InvalidNumericField_LogsErrorWithRowInfo()
        {
            var table = new DataTable();
            table.Columns.Add("Quantity");
            table.Rows.Add("abc");
            ErrorLogger.Clear();
            CsvNormalizer.NormalizeDataTable(table);
            Assert.Single(ErrorLogger.Errors);
            Assert.Contains("Row 1: The column 'Quantity' expected a numeric value", ErrorLogger.Errors[0]);
        }

        [Fact]
        public void SummaryCountsRepeatedErrors()
        {
            ErrorLogger.Clear();
            ErrorLogger.LogError("sample");
            ErrorLogger.LogError("sample");
            Assert.True(ErrorLogger.ErrorSummary.TryGetValue("sample", out var count));
            Assert.Equal(2, count);
        }
    }
}
