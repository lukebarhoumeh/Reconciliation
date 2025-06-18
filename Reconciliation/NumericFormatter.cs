namespace Reconciliation
{
    internal static class NumericFormatter
    {
        internal static string Money(decimal d) => d.ToString("0.##");
        internal static string Percent(decimal d) => d.ToString("0.##");
    }
}
