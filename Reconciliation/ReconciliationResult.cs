using System.Data;

namespace Reconciliation;

public record ReconciliationSummary(int TotalRows, int UnmatchedGroups, decimal OverBill, decimal UnderBill);

public record ReconciliationResult(DataTable Exceptions, ReconciliationSummary Summary);
