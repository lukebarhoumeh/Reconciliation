using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Reconciliation;

/// <summary>
/// Evaluates simple arithmetic expressions with column placeholders.
/// </summary>
public static class ExpressionColumnBuilder
{
    private static readonly Regex Placeholder = new Regex(@"\{([A-Za-z0-9_]+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Evaluate an expression using values from the given data row.
    /// Supports +, -, *, / operators.
    /// </summary>
public static decimal Evaluate(string expression, DataRow row, DataRow? fallback = null)
{
    if (string.IsNullOrWhiteSpace(expression))
        return 0m;
    string replaced = Placeholder.Replace(expression, m =>
    {
        string col = m.Groups[1].Value;
        DataRow? target = row.Table.Columns.Contains(col) ? row : fallback;
        if (target == null || !target.Table.Columns.Contains(col))
            return "0";
        string val = Convert.ToString(target[col]) ?? "0";
        return string.IsNullOrWhiteSpace(val) ? "0" : val;
    });
        try
        {
            var result = new DataTable().Compute(replaced, string.Empty);
            decimal.TryParse(Convert.ToString(result), NumberStyles.Any, CultureInfo.InvariantCulture, out var d);
            return d;
        }
        catch
        {
            return 0m;
        }
    }
}
