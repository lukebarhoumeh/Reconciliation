using System;
using System.Data;
using Reconciliation;

namespace Reconciliation.Tests
{
    public class SchemaValidatorTests
    {
        [Fact]
        public void RequireColumns_ThrowsAndLogs_WhenMissing()
        {
            var dt = new DataTable();
            dt.Columns.Add("A");
            ErrorLogger.Clear();
            Assert.Throws<ArgumentException>(() => SchemaValidator.RequireColumns(dt, "file.csv", new[] { "A", "B" }));
            Assert.Contains(ErrorLogger.Entries, e => e.ColumnName == "B" && e.FileName == "file.csv");
        }

        [Fact]
        public void RequireColumns_Passes_WhenAllPresent()
        {
            var dt = new DataTable();
            dt.Columns.Add("A");
            dt.Columns.Add("B");
            ErrorLogger.Clear();
            SchemaValidator.RequireColumns(dt, "file.csv", new[] { "A", "B" });
            Assert.Empty(ErrorLogger.Entries);
        }

    }
}
