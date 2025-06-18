using System;
using System.Data;

namespace Reconciliation
{
    /// <summary>
    /// Provides invoice reconciliation between MSP Hub and Microsoft invoices.
    /// </summary>
    public class ReconciliationService
    {
        /// <summary>Returns rows where any column value differs.</summary>
        public DataTable CompareInvoices(DataTable sixDotOne, DataTable microsoft)
        {
            if (sixDotOne == null) throw new ArgumentNullException(nameof(sixDotOne));
            if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));

            var detector = new DiscrepancyDetector();
            detector.Compare(sixDotOne, microsoft);
            return detector.GetMismatches();
        }
    }
}
