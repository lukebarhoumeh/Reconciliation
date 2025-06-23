using System;
using System.Data;
using Reconciliation;

namespace Reconciliation.Tests
{
    public class ReconciliationServiceTests
    {
        private static DataTable CreateMspHubTable()
        {
            string[] cols =
            {
                "CustomerName","CustomerDomainName","ProductId","ProductName","SkuId","ChargeType","SubscriptionStartDate","SubscriptionEndDate","EffectiveUnitPrice","Quantity","OrderDate","Term","BillingCycle","MSRPPrice","EffectiveMSRPPrice","ChargeStartDate","ChargeEndDate","PartnerEffectiveUnitPrice","AdminDiscountPercentage","PartnerDiscountPercentage","Total","EffectiveDays","PartnerUnitPrice","TotalDays","UnitPrice"
            };
            var dt = new DataTable();
            foreach (var c in cols) dt.Columns.Add(c);
            return dt;
        }

        private static DataTable CreateMicrosoftTable()
        {
            string[] cols =
            {
                "CustomerName","CustomerDomainName","ProductId","ProductName","SkuId","ChargeType","SubscriptionStartDate","SubscriptionEndDate","EffectiveUnitPrice","Quantity","OrderDate","Term","BillingCycle","ChargeStartDate","ChargeEndDate","Total","SubscriptionDescription"
            };
            var dt = new DataTable();
            foreach (var c in cols) dt.Columns.Add(c);
            return dt;
        }

        [Fact]
        public void CompareInvoices_DetectsMissingRows()
        {
            var hub = CreateMspHubTable();
            hub.Rows.Add(
                "Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31",
                "1","1","2024-01-01","T","M","1","1","2024-01-01","2024-01-31","1",
                "10","9","1","30","1","30","1");
            var ms = CreateMicrosoftTable();
            ms.Rows.Add(
                "Other","other.com","p2","Other","s2","Usage","2024-01-01","2024-01-31",
                "1","1","2024-01-01","T","M","2024-01-01","2024-01-31","1","Service");

            var svc = new ReconciliationService();
            var result = svc.CompareInvoices(hub, ms);

            Assert.Equal(5, result.Rows.Count);
            var cols = result.Rows.Cast<DataRow>().Select(r => r["Field Name"].ToString());
            Assert.Contains("CustomerName", cols);
            Assert.Contains("Customer Website", cols);
            Assert.Contains("ProductName", cols);
        }

        [Fact]
        public void CompareInvoices_NoMismatch_WhenRowsMatch()
        {
            var hub = CreateMspHubTable();
            hub.Rows.Add("Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31","1","1","2024-01-01","T","M","1","1","2024-01-01","2024-01-31","1","10","9","1","30","1","30","1");
            var ms = CreateMicrosoftTable();
            ms.Rows.Add("Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31","1","1","2024-01-01","T","M","2024-01-01","2024-01-31","1","Service");
            var svc = new ReconciliationService();
            var result = svc.CompareInvoices(hub, ms);
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void CompareInvoices_FlagsPriceRules()
        {
            var hub = CreateMspHubTable();
            hub.Rows.Add(
                "Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31",
                "1","1","2024-01-01","T","M","10","8","2024-01-01","2024-01-31","2",
                "20","5","1","15","3","30","1");
            var ms = CreateMicrosoftTable();
            ms.Rows.Add(
                "Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31",
                "1","1","2024-01-01","T","M","2024-01-01","2024-01-31","1","Service");

            var svc = new ReconciliationService();
            var result = svc.CompareInvoices(hub, ms);

            Assert.Empty(result.Rows);
        }
    }
}
