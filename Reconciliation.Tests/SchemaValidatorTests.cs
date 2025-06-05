using System;
using System.Data;
using System.Collections.Generic;
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

        public static IEnumerable<object[]> SkuVariants => new List<object[]>
        {
            new object[]{"SkuName"},
            new object[]{"SKU"},
            new object[]{"sku_id"},
            new object[]{"Sku"}
        };

        [Theory]
        [MemberData(nameof(SkuVariants))]
        public void RequireColumns_MapsSkuIdVariants(string variant)
        {
            var dt = new DataTable();
            dt.Columns.Add(variant);
            SchemaValidator.RequireColumns(dt, "file.csv", new[] { "SkuId" }, true);
            Assert.Contains("SkuId", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        }

        [Fact]
        public void RequireColumns_SuggestsAlternative()
        {
            var dt = new DataTable();
            dt.Columns.Add("ProductName");
            var ex = Assert.Throws<ArgumentException>(() => SchemaValidator.RequireColumns(dt, "file.csv", new[] { "ProductId" }, true));
            Assert.Contains("Did you mean", ex.Message);
        }
    }
}
