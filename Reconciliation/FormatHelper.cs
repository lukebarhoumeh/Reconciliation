namespace Reconciliation
{
    internal static class FormatHelper
    {
        internal static string FormatMoney(decimal value)
            => value.ToString("0.##");

        internal static string FormatPercent(decimal value)
            => value.ToString("0.##");
    }
}
