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
            Assert.Throws<ArgumentException>(() => SchemaValidator.RequireColumns(dt, "file.csv", new[] { "A", "B" }, false));
            Assert.Contains(ErrorLogger.Entries, e => e.ColumnName == "B" && e.FileName == "file.csv");
        }

        [Fact]
        public void RequireColumns_FuzzyRenamesColumn()
        {
            var dt = new DataTable();
            dt.Columns.Add("ProdutId");
            SchemaValidator.RequireColumns(dt, "file.csv", new[] { "ProductId" }, true);
            Assert.Contains("ProductId", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        }
    }
}
