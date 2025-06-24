using System.Data;
using Reconciliation;

namespace Reconciliation.Tests;

public class ExpressionColumnBuilderTests
{
    [Fact]
    public void Evaluate_ReplacesPlaceholdersAndComputesExpression()
    {
        DataTable table = new();
        table.Columns.Add("A");
        table.Columns.Add("B");
        var row = table.NewRow();
        row["A"] = "1";
        row["B"] = "3";
        table.Rows.Add(row);

        decimal result = ExpressionColumnBuilder.Evaluate("{A}+{B}*2", row);
        Assert.Equal(7m, result);
    }

    [Fact]
    public void MissingColumnDefaultsToZero()
    {
        DataTable table = new();
        table.Columns.Add("A");
        var row = table.NewRow();
        row["A"] = "5";
        table.Rows.Add(row);

        decimal result = ExpressionColumnBuilder.Evaluate("{A}+{B}", row);
        Assert.Equal(5m, result);
    }
}
