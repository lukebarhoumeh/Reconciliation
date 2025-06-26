using System.Data;
using Reconciliation;

namespace Reconciliation.Tests;

public class AdvancedReconciliationTests
{
    private static readonly string[] MsColumns =
    {
        "PartnerId","CustomerId","CustomerName","CustomerDomainName","CustomerCountry",
        "InvoiceNumber","MpnId","Tier2MpnId","OrderId","OrderDate","ReferenceId",
        "ProductId","SkuId","AvailabilityId","SkuName","ProductName",
        "ChargeType","UnitPrice","Quantity","Subtotal","TaxTotal","Total","Currency",
        "PriceAdjustmentDescription",
        "PublisherName","PublisherId","SubscriptionDescription","SubscriptionId",
        "ChargeStartDate","ChargeEndDate","TermAndBillingCycle","EffectiveUnitPrice",
        "UnitType","AlternateId","BillableQuantity","BillingFrequency","PricingCurrency",
        "PCToBCExchangeRate","PCToBCExchangeRateDate","MeterDescription","ReservationOrderId",
        "CreditReasonCode","SubscriptionStartDate","SubscriptionEndDate","ProductQualifiers",
        "PromotionId","ProductCategory","Term","BillingCycle"
    };

    [Fact]
    public void MapsPartnerToMicrosoftSchema()
    {
        DataTable dt = new();
        dt.Columns.Add("PartnerInvoiceNumber");
        dt.Columns.Add("PartNumber");
        dt.Columns.Add("Quantity");
        dt.Rows.Add("INV", "SKU", "1");
        var mapped = CsvSchemaMapper.Normalize(dt, SourceType.Partner);
        Assert.Equal(MsColumns, mapped.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
    }

    [Fact(Skip = "TODO: fix offset logic")]
    public void CreditAndDebitNetToZero()
    {
        DataTable ms = CreateMsTable();
        ms.Rows.Add("C1","S1","P1","SKU1","Usage","2024-01-01","2024-01-31","1","10");
        DataTable partner = CreateMsTable();
        partner.Rows.Add("C1","S1","P1","SKU1","Usage","2024-01-01","2024-01-31","-1","-10");
        var svc = new AdvancedReconciliationService();
        var res = svc.Reconcile(ms, partner);
        Console.WriteLine($"rows={res.Exceptions.Rows.Count}");
        foreach (DataRow r in res.Exceptions.Rows) Console.WriteLine(r["Discrepancy"]);
        Assert.Empty(res.Exceptions.Rows);
    }

    [Fact]
    public void DetectsMissingRows()
    {
        DataTable ms = CreateMsTable();
        ms.Rows.Add("C1","S1","P1","SKU1","Usage","2024-01-01","2024-01-31","1","10");
        DataTable partner = CreateMsTable();
        var svc = new AdvancedReconciliationService();
        var res = svc.Reconcile(ms, partner);
        Assert.Contains(res.Exceptions.AsEnumerable(), r => r["Discrepancy"].ToString() == "MISSING_IN_PARTNER");
    }

    [Fact]
    public void DistributorMapping_EvaluatesExpressions()
    {
        DataTable raw = new();
        raw.Columns.Add("CustomerUnitPrice");
        raw.Columns.Add("Quantity");
        raw.Columns.Add("CustomerTax");
        raw.Rows.Add("2", "3", "1");
        var mapped = CsvSchemaMapper.Normalize(raw, SourceType.Distributor);
        Assert.Equal("6", mapped.Rows[0]["Subtotal"]);
        Assert.Equal("7", mapped.Rows[0]["Total"]);
    }

    [Fact]
    public void KeyExpansion_ReducesMissingRows()
    {
        DataTable ms = CreateMsTable();
        var r1 = ms.NewRow();
        r1["CustomerId"] = "S1";
        r1["SubscriptionId"] = "SUB";
        r1["ProductId"] = "P1";
        r1["SkuId"] = "SKU1";
        r1["ChargeType"] = "Usage";
        r1["OrderId"] = "O1";
        r1["InvoiceNumber"] = "INV1";
        r1["Quantity"] = "1";
        r1["Total"] = "10";
        ms.Rows.Add(r1);

        DataTable other = CreateMsTable();
        var r2 = other.NewRow();
        r2["CustomerId"] = "S1";
        r2["SubscriptionId"] = "SUB";
        r2["ProductId"] = "P1";
        r2["SkuId"] = "SKU1";
        r2["ChargeType"] = "Usage";
        r2["OrderId"] = "O1";
        r2["InvoiceNumber"] = "INV1";
        r2["Quantity"] = "1";
        r2["Total"] = "10";
        other.Rows.Add(r2);
        var svc = new AdvancedReconciliationService();
        var res = svc.Reconcile(ms, other);
        Assert.Empty(res.Exceptions.Rows);
    }

    [Fact]
    public void EffectiveUnitPriceComparison_UsesEUP()
    {
        DataTable ms = CreateMsTable();
        var msRow = ms.NewRow();
        msRow["CustomerId"] = "S1";
        msRow["SubscriptionId"] = "SUB";
        msRow["ProductId"] = "P1";
        msRow["SkuId"] = "SKU1";
        msRow["ChargeType"] = "Usage";
        msRow["EffectiveUnitPrice"] = "10";
        msRow["UnitPrice"] = "20";
        msRow["Quantity"] = "1";
        ms.Rows.Add(msRow);

        DataTable other = CreateMsTable();
        var oRow = other.NewRow();
        oRow["CustomerId"] = "S1";
        oRow["SubscriptionId"] = "SUB";
        oRow["ProductId"] = "P1";
        oRow["SkuId"] = "SKU1";
        oRow["ChargeType"] = "Usage";
        oRow["EffectiveUnitPrice"] = "10";
        oRow["UnitPrice"] = "8";
        oRow["Quantity"] = "1";
        other.Rows.Add(oRow);
        var svc = new AdvancedReconciliationService();
        var res = svc.Reconcile(ms, other);
        Assert.Empty(res.Exceptions.Rows);
    }

    private static DataTable CreateMsTable()
    {
        DataTable dt = new();
        foreach (var col in MsColumns)
            dt.Columns.Add(col);
        return dt;
    }
}
