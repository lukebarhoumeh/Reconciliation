using System;
using System.Data;

namespace Reconciliation
{
    /// <summary>
    /// Result of invoice validation containing invalid rows and error counts.
    /// </summary>
    public class InvoiceValidationResult
    {
        public InvoiceValidationResult(DataTable invalidRows, int highPriority, int lowPriority)
        {
            InvalidRows = invalidRows ?? throw new ArgumentNullException(nameof(invalidRows));
            HighPriority = highPriority;
            LowPriority = lowPriority;
        }

        /// <summary>
        /// Table containing invalid invoice rows.
        /// </summary>
        public DataTable InvalidRows { get; }

        /// <summary>
        /// Convenience view over <see cref="InvalidRows"/>.
        /// </summary>
        public DataView InvalidRowsView => InvalidRows.DefaultView;

        /// <summary>
        /// Count of high priority errors.
        /// </summary>
        public int HighPriority { get; }

        /// <summary>
        /// Count of low priority errors.
        /// </summary>
        public int LowPriority { get; }
    }
}
