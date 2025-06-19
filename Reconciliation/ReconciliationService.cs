using System;
using System.Data;

namespace Reconciliation
{
    /// <summary>
    /// Provides invoice reconciliation between MSP Hub and Microsoft invoices.
    /// </summary>
    public class ReconciliationService
    {
        public string LastSummary { get; private set; } = string.Empty;

        /// <summary>Returns rows where any column value differs.</summary>
        public DataTable CompareInvoices(DataTable sixDotOne, DataTable microsoft)
        {
            if (sixDotOne == null) throw new ArgumentNullException(nameof(sixDotOne));
            if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));

            var detector = new DiscrepancyDetector();
            detector.Compare(sixDotOne, microsoft);
            LastSummary = string.Join(", ", detector.Summary.Select(kv => $"{kv.Value} {kv.Key}"));
            var table = detector.GetMismatches();
            if (!table.Columns.Contains("Reason"))
                table.Columns.Add("Reason", typeof(string));
            return table;
        }
    }
}
