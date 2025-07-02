using System.Data;
using Reconciliation;

namespace Reconciliation.Tests;

public class BusinessKeyReconciliationServiceTests
{
    private static DataTable CreateTable(bool microsoft = false)
    {
        string[] cols = microsoft
            ? new[] { "DomainUrl", "ProductGuid", "UnitPrice", "Subtotal", "Total", "Quantity" }
            : new[] { "CustomerDomainName", "ProductId", "PartnerUnitPrice", "PartnerSubTotal", "PartnerTotal", "Quantity" };
        var dt = new DataTable();
        foreach (var c in cols) dt.Columns.Add(c);
        return dt;
    }

    private static DataTable Table(params (string Col, string Val)[] cells)
    {
        var dt = new DataTable();
        foreach (var (col, _) in cells)
            if (!dt.Columns.Contains(col))
                dt.Columns.Add(col);
        var row = dt.NewRow();
        foreach (var (col, val) in cells)
            row[col] = val;
        dt.Rows.Add(row);
        return dt;
    }

    [Fact]
    public void Reconcile_DetectsMissingRows()
    {
        var ours = CreateTable();
        ours.Rows.Add("cust.com","P1","1","1","10","1");
        var ms = CreateTable(true);

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Missing in Microsoft", result.Rows[0]["Status"]);
    }

    [Fact]
    public void Reconcile_DetectsMismatchedTotal()
    {
        var ours = CreateTable();
        ours.Rows.Add("cust.com","P1","1","1","10","1");
        var ms = CreateTable(true);
        ms.Rows.Add("cust.com","P1","1","1","12","1");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Mismatched", result.Rows[0]["Status"]);
    }

    [Fact]
    public void Reconcile_HappyPath_NoDifferences()
    {
        var ours = CreateTable();
        ours.Rows.Add("cust.com","P1","1","1","10","1");
        var ms = CreateTable(true);
        ms.Rows.Add("cust.com","P1","1","1","10","1");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Matched", result.Rows[0]["Status"]);
    }

    [Fact]
    public void Reconcile_IgnoresChargeStartDate()
    {
        var ours = Table(("CustomerDomainName","cust.com"), ("ProductId","P1"),
                         ("ChargeStartDate","2024-01-01"), ("Total","10"));
        var ms   = Table(("DomainUrl","cust.com"), ("ProductGuid","P1"),
                         ("ChargeStartDate","2024-02-02"), ("Total","10"));

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Matched", result.Rows[0]["Status"]);
    }

    [Fact]
    public void BusinessKeyReconciliation_AliasesWork()
    {
        var ours = CreateTable();
        ours.Rows.Add("cust.com","P1","1","1","10","1");
        var ms = CreateTable(true);
        ms.Rows.Add("cust.com","P1","1","1","10","1");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Matched", result.Rows[0]["Status"]);
    }

    [Fact]
    public void BusinessKeyReconciliation_AliasesAndTenantFilter()
    {
        var ours = CreateTable();
        ours.Columns.Add("PartnerId");
        ours.Rows.Add("cust.com","P1","1","1","10","1","T1");

        var ms = new DataTable();
        foreach (var c in new[]{"DomainUrl","ProductGuid","UnitPrice","Subtotal","Total","Quantity","PartnerId"})
            ms.Columns.Add(c);
        ms.Rows.Add("cust.com","P1","1","1","10","1","T1");
        ms.Rows.Add("cust.com","P1","1","1","10","1","T2");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Matched", result.Rows[0]["Status"]);
        Assert.Equal("Matched: 1 | Missing in Microsoft: 0 | Mismatched: 0 | Data Errors: 0", svc.LastSummary);
    }

    [Fact]
    public void AliasAndFallback_MatchesRows()
    {
        var ours = Table(("SubId","123"), ("ProductId","P1"),
                         ("CustomerDomainName","foo.com"), ("UnitPrice","10"));

        var ms   = Table(("SubscriptionGuid","123"), ("ProductId","P1"),
                         ("CustomerDomainName","foo.com"), ("PartnerUnitPrice","10"));

        var svc = new BusinessKeyReconciliationService();
        var diff = svc.Reconcile(ours, ms);

        Assert.Single(diff.Rows);
        Assert.Equal("Matched", diff.Rows[0]["Status"]);
        Assert.Equal("Matched: 1 | Missing in Microsoft: 0 | Mismatched: 0 | Data Errors: 0", svc.LastSummary);
    }

    [Fact]
    public void TenantFilter_CaseInsensitive()
    {
        var ours = CreateTable();
        ours.Rows.Add("foo.com","P1","1","1","10","1");

        var ms = CreateTable(true);
        ms.Rows.Add("FOO.COM","P1","1","1","10","1");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Matched", result.Rows[0]["Status"]);
        Assert.Equal("Matched: 1 | Missing in Microsoft: 0 | Mismatched: 0 | Data Errors: 0", svc.LastSummary);
    }

    [Fact]
    public void Reconcile_AggregatesAcrossDuplicateRows()
    {
        var ours = CreateTable();
        ours.Rows.Add("dup.com","P1","1","1","10","1");
        ours.Rows.Add("dup.com","P1","1","1","10","1");

        var ms = CreateTable(true);
        ms.Rows.Add("dup.com","P1","1","1","10","1");
        ms.Rows.Add("dup.com","P1","1","1","10","1");

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Matched", result.Rows[0]["Status"]);
        Assert.Equal("Matched: 1 | Missing in Microsoft: 0 | Mismatched: 0 | Data Errors: 0", svc.LastSummary);
    }

    [Fact]
    public void Reconcile_IgnoresRowsOnlyInMicrosoft()
    {
        var ours = CreateTable();
        ours.Rows.Add("cust.com","P1","1","1","10","1");

        var ms = CreateTable(true);
        ms.Rows.Add("cust.com","P1","1","1","10","1");
        ms.Rows.Add("cust.com","P2","1","1","10","1"); // extra row only in MS

        var svc = new BusinessKeyReconciliationService();
        var result = svc.Reconcile(ours, ms);

        Assert.Single(result.Rows);
        Assert.Equal("Matched", result.Rows[0]["Status"]);
        Assert.Equal("Matched: 1 | Missing in Microsoft: 0 | Mismatched: 0 | Data Errors: 0", svc.LastSummary);
    }
}

