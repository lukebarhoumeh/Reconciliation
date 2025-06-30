using System.Data;
using Reconciliation;

namespace Reconciliation.Tests;

public class BusinessKeyReconciliationServiceTests
{
    private static DataTable CreateTable()
    {
        string[] cols =
        {
            "CustomerDomainName","ProductId","ChargeType","ChargeStartDate","SubscriptionId",
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
        var ms = CreateTable();

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
        var ms = CreateTable();
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
        var ms = CreateTable();
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
        var ms = CreateTable();
        ms.Rows.Add("cust.com","P1","Usage","2024-01-02","SUB1","1","1","10","1");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Empty(result.Rows);
    }
}
