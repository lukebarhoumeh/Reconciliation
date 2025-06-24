using System.Data;
using Reconciliation;

namespace Reconciliation.Tests;

public class AdvancedReconciliationTests
{
    private static readonly string[] MsColumns =
    {
        "PartnerId","CustomerId","CustomerName","CustomerDomainName","CustomerCountry",
        "InvoiceNumber","MpnId","Tier2MpnId","OrderId","OrderDate","ProductId","SkuId",
        "AvailabilityId","SkuName","ProductName","ChargeType","UnitPrice","Quantity",
        "Subtotal","TaxTotal","Total","Currency","PriceAdjustmentDescription",
        "PublisherName","PublisherId","SubscriptionDescription","SubscriptionId",
        "ChargeStartDate","ChargeEndDate","TermAndBillingCycle","EffectiveUnitPrice",
        "UnitType","AlternateId","BillableQuantity","BillingFrequency","PricingCurrency",
        "PCToBCExchangeRate","PCToBCExchangeRateDate","MeterDescription","ReservationOrderId",
        "CreditReasonCode","SubscriptionStartDate","SubscriptionEndDate","ReferenceId",
        "ProductQualifiers","PromotionId","ProductCategory"
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

    [Fact]
    public void CreditAndDebitNetToZero()
    {
        DataTable ms = CreateMsTable();
        ms.Rows.Add("C1","S1","P1","SKU1","Usage","2024-01-01","2024-01-31","1","10");
        DataTable partner = CreateMsTable();
        partner.Rows.Add("C1","S1","P1","SKU1","Usage","2024-01-01","2024-01-31","-1","-10");
        var svc = new AdvancedReconciliationService();
        var res = svc.Reconcile(ms, partner);
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

    private static DataTable CreateMsTable()
    {
        DataTable dt = new();
        foreach (var col in MsColumns)
            dt.Columns.Add(col);
        return dt;
    }
}
