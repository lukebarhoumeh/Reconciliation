using System;
using System.Data;

namespace Reconciliation
{
    /// <summary>
    /// Orchestrates end‑to‑end reconciliation:
    ///   • Row‑level (presence / field mismatches) via BusinessKeyReconciliationService
    ///   • Price & quantity mismatches via PriceMismatchService
    /// and returns both result tables plus a summary string.
    /// </summary>
    public class AdvancedReconciliationService
    {
        private readonly BusinessKeyReconciliationService _rowComparer = new();
        private readonly PriceMismatchService _priceComparer = new();

        public bool HideMissingInHub
        {
            get => _rowComparer.HideMissingInHub;
            set => _rowComparer.HideMissingInHub = value;
        }

        /// <summary>
        /// Runs both reconciliation passes and returns the aggregated result.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public AdvancedReconciliationResult Reconcile(
            DataTable msphub,
            DataTable microsoft)
        {
            if (msphub == null) throw new ArgumentNullException(nameof(msphub));
            if (microsoft == null) throw new ArgumentNullException(nameof(microsoft));

            // Pass 1 – row‑level reconciliation
            DataTable rowDiscrepancies = _rowComparer.Reconcile(msphub, microsoft);

            // Pass 2 – price / quantity reconciliation
            DataTable priceMismatches = _priceComparer.GetPriceMismatches(msphub, microsoft);

            return new AdvancedReconciliationResult
            {
                RowDiscrepancies = rowDiscrepancies,
                PriceMismatches = priceMismatches,
                Summary = _rowComparer.LastSummary
            };
        }
    }

    /// <summary>
    /// Simple DTO carrying both result tables and an overall summary.
    /// </summary>
    public sealed class AdvancedReconciliationResult
    {
        public DataTable RowDiscrepancies { get; init; } = new();
        public DataTable PriceMismatches { get; init; } = new();
        public string Summary { get; init; } = string.Empty;
    }
}
