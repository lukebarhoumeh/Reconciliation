using Reconciliation;
using System.Data;
using System.Linq;
using Xunit;

namespace Reconciliation.Tests
{
    public class ErrorSummaryLargeFileTests
    {
        [Fact]
        public void ErrorLogger_SummarizesLargeInput()
        {
            var table = new DataTable();
            table.Columns.Add("Quantity");
            for (int i = 0; i < 20; i++)
            {
                table.Rows.Add("bad");
            }
            ErrorLogger.Clear();
            ErrorLogger.MaxDetailedRows = 5;
            CsvNormalizer.NormalizeDataTable(table);
            var summary = ErrorLogger.Entries.Last();
            Assert.True(summary.IsSummary);
            Assert.Contains("additional rows", summary.Description);
        }
    }
}
