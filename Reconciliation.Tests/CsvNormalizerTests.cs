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
            ErrorLogger.Log("test message");
            Assert.Single(ErrorLogger.Errors);
        }
    }
}
