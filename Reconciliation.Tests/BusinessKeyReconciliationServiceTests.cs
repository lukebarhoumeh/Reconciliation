using System.Data;
using Reconciliation;

namespace Reconciliation.Tests;

public class BusinessKeyReconciliationServiceTests
{
    private static DataTable CreateTable(bool useAliases = false)
    {
        string[] cols =
        {
            useAliases ? "DomainUrl" : "CustomerDomainName",
            useAliases ? "ProductGuid" : "ProductId",
            "ChargeType",
            "ChargeStartDate",
            useAliases ? "SubscriptionGuid" : "SubscriptionId",
            "UnitPrice","Subtotal","Total","Quantity"
        };
        var dt = new DataTable();
        foreach (var c in cols) dt.Columns.Add(c);
        return dt;
    }

    [Fact]
    public void Reconcile_DetectsMissingRows()
    {
        var ours = CreateTable();
        ours.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","10","1");
        var ms = CreateTable(true);

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Missing in Microsoft", result.Rows[0]["Explanation"]);
    }

    [Fact]
    public void Reconcile_DetectsMismatchedTotal()
    {
        var ours = CreateTable();
        ours.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","10","1");
        var ms = CreateTable(true);
        ms.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","12","1");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Total", result.Rows[0]["Field Name"]);
        Assert.Contains("Mismatch in Total", result.Rows[0]["Explanation"].ToString());
    }

    [Fact]
    public void Reconcile_HappyPath_NoDifferences()
    {
        var ours = CreateTable();
        ours.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","10","1");
        var ms = CreateTable(true);
        ms.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","10","1");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Reconcile_AllowsOneDayDateDifference()
    {
        var ours = CreateTable();
        ours.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","10","1");
        var ms = CreateTable(true);
        ms.Rows.Add("cust.com","P1","Usage","2024-01-02","SUB1","1","1","10","1");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Empty(result.Rows);
    }

    [Fact]
    public void BusinessKeyReconciliation_AliasesWork()
    {
        var ours = CreateTable();
        ours.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","10","1");
        var ms = CreateTable(true);
        ms.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","10","1");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Empty(result.Rows);
    }

    [Fact]
    public void BusinessKeyReconciliation_AliasesAndTenantFilter()
    {
        var ours = CreateTable();
        ours.Columns.Add("PartnerId");
        ours.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","10","1","T1");

        var ms = new DataTable();
        foreach (var c in new[]{"DomainUrl","ProductGuid","ChargeType","ChargeStartDate","SubscriptionGUID","UnitPrice","Subtotal","Total","Quantity","PartnerId"})
            ms.Columns.Add(c);
        ms.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","10","1","T1");
        ms.Rows.Add("cust.com","P1","Usage","2024-01-01","SUB1","1","1","10","1","T2");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Empty(result.Rows);
        Assert.Equal("Perfect: 1 | OnlyMSP: 0 | OnlyMS: 0 | Diff: 0", svc.LastSummary);
    }
}

