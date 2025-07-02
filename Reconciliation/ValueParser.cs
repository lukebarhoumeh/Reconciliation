namespace Reconciliation
{
    internal static class ValueParser
    {
        internal static decimal SafeDecimal(object? value)
        {
            return decimal.TryParse(Convert.ToString(value), System.Globalization.NumberStyles.Any,
                                     System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m;
        }
    }
}
