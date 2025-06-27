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
                "InvoiceNumber","CustomerName","CustomerDomainName","ProductId","ProductName","SkuId","ChargeType","SubscriptionStartDate","SubscriptionEndDate","EffectiveUnitPrice","Quantity","OrderDate","Term","BillingCycle","MSRPPrice","EffectiveMSRPPrice","ChargeStartDate","ChargeEndDate","PartnerEffectiveUnitPrice","AdminDiscountPercentage","PartnerDiscountPercentage","Subtotal","TaxTotal","Total","EffectiveDays","PartnerUnitPrice","TotalDays","UnitPrice"
            };
            var dt = new DataTable();
            foreach (var c in cols) dt.Columns.Add(c);
            return dt;
        }

        private static DataTable CreateMicrosoftTable()
        {
            string[] cols =
            {
                "InvoiceNumber","CustomerName","CustomerDomainName","ProductId","ProductName","SkuId","ChargeType","SubscriptionStartDate","SubscriptionEndDate","EffectiveUnitPrice","Quantity","OrderDate","Term","BillingCycle","ChargeStartDate","ChargeEndDate","Subtotal","TaxTotal","Total","SubscriptionDescription"
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
                "INV1","Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31",
                "1","1","2024-01-01","T","M","1","1","2024-01-01","2024-01-31","1",
                "5","1","10","9","1","30","1","30","1");
            var ms = CreateMicrosoftTable();
            ms.Rows.Add(
                "INV2","Other","other.com","p2","Other","s2","Usage","2024-01-01","2024-01-31",
                "1","1","2024-01-01","T","M","2024-01-01","2024-01-31","5","0","1","Service");

            var svc = new ReconciliationService();
            var result = svc.CompareInvoices(hub, ms);

            Assert.Equal(2, result.Rows.Count);
            var messages = result.Rows.Cast<DataRow>().Select(r => r["Explanation"].ToString());
            Assert.Contains("Missing in Microsoft", messages);
            Assert.Contains("Missing in MSPHub", messages);
        }

        [Fact]
        public void CompareInvoices_NoMismatch_WhenRowsMatch()
        {
            var hub = CreateMspHubTable();
            hub.Rows.Add("INV1","Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31","1","1","2024-01-01","T","M","1","1","2024-01-01","2024-01-31","1","5","1","5","0","10","30","1","30","1");
            var ms = CreateMicrosoftTable();
            ms.Rows.Add("INV1","Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31","1","1","2024-01-01","T","M","2024-01-01","2024-01-31","5","0","10","Service");
            var svc = new ReconciliationService();
            var result = svc.CompareInvoices(hub, ms);
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void CompareInvoices_FlagsPriceRules()
        {
            var hub = CreateMspHubTable();
            hub.Rows.Add(
                "INV1","Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31",
                "1","2","2024-01-01","T","M","10","8","2024-01-01","2024-01-31","2",
                "7","1","7","1","7","30","1","30","1");
            var ms = CreateMicrosoftTable();
            ms.Rows.Add(
                "INV1","Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31",
                "1","1","2024-01-01","T","M","2024-01-01","2024-01-31","5","0","3","Service");

            var svc = new ReconciliationService();
            var result = svc.CompareInvoices(hub, ms);

            Assert.Equal(4, result.Rows.Count);
            var fields = result.Rows.Cast<DataRow>().Select(r => r["Field Name"].ToString());
            Assert.Contains("Quantity", fields);
            Assert.Contains("Subtotal", fields);
            Assert.Contains("TaxTotal", fields);
            Assert.Contains("Total", fields);
        }

        [Fact]
        public void CompareInvoices_HandlesDuplicateKeys()
        {
            var hub = CreateMspHubTable();
            // two MSPHub rows with the same InvoiceNumber and SkuId
            hub.Rows.Add(
                "INV1","Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31",
                "1","1","2024-01-01","T","M","1","1","2024-01-01","2024-01-31","1",
                "5","1","5","0","10","30","1","30","1");
            hub.Rows.Add(
                "INV1","Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31",
                "1","2","2024-01-01","T","M","10","8","2024-01-01","2024-01-31","2",
                "7","1","7","1","7","30","1","30","1");

            var ms = CreateMicrosoftTable();
            // two Microsoft rows with the same key
            ms.Rows.Add(
                "INV1","Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31",
                "1","1","2024-01-01","T","M","2024-01-01","2024-01-31","5","0","10","Service");
            ms.Rows.Add(
                "INV1","Cust","cust.com","p1","Prod","s1","Usage","2024-01-01","2024-01-31",
                "1","1","2024-01-01","T","M","2024-01-01","2024-01-31","5","0","10","Service");

            var svc = new ReconciliationService();
            var result = svc.CompareInvoices(hub, ms);

            // Second hub row mismatches all four comparable fields against each Microsoft row
            Assert.Equal(8, result.Rows.Count);
            Assert.All(result.Rows.Cast<DataRow>(), r =>
                Assert.Contains("Hub row", r["Explanation"].ToString()));
        }

        [Fact]
        public void TryParseMoney_ParsesScientificNotation()
        {
            bool ok = ReconciliationService.TryParseMoney("0E-20", out var d);
            Assert.True(ok);
            Assert.Equal(0m, d);
        }

        [Fact]
        public void BlankTaxTotal_DoesNotLogError()
        {
            var table = new DataTable();
            table.Columns.Add("TaxTotal");
            table.Rows.Add("");
            ErrorLogger.Clear();
            CsvNormalizer.NormalizeDataTable(table);
            Assert.Empty(ErrorLogger.Entries);
        }
    }
}
