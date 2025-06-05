using Reconciliation;
using System.IO;
using System.Linq;

namespace Reconciliation.Tests
{
    public class ErrorLoggerDedupTests
    {
        [Fact]
        public void LogsSummaryAfterThreshold()
        {
            ErrorLogger.Clear();
            ErrorLogger.MaxDetailedRows = 2;
            for (int i = 0; i < 5; i++)
            {
                ErrorLogger.LogError(i + 1, "Col", "Issue", "bad", "f.csv", "ctx");
            }

            Assert.Equal(3, ErrorLogger.Entries.Count);
            var summary = ErrorLogger.Entries.Last();
            Assert.True(summary.IsSummary);
            Assert.Contains("3 additional rows", summary.Description);
        }
    }
}
