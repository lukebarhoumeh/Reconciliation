using System;
using System.Data;
using Reconciliation;

namespace Reconciliation.Tests
{
    public class InvoiceValidationServiceTests
    {
        private static DataTable CreateTable()
        {
            string[] cols =
            {
                "ChargeStartDate","ChargeEndDate","MSRPPrice","EffectiveDays",
                "PartnerDiscountPercentage","CustomerDiscountPercentage","Total","CustomerSubtotal"
            };
            var dt = new DataTable();
            foreach (var c in cols) dt.Columns.Add(c);
            return dt;
        }

        [Fact]
        public void ValidateInvoice_NoErrors_ReturnsEmpty()
        {
            var dt = CreateTable();
            dt.Rows.Add("2024-01-01","2024-01-30","4","30","19","5","10","10");
            var svc = new InvoiceValidationService();
            var result = svc.ValidateInvoice(dt);
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void ValidateInvoice_EffectiveDaysError()
        {
            var dt = CreateTable();
            dt.Rows.Add("2024-01-01","2024-01-30","4","10","19","5","10","10");
            var result = new InvoiceValidationService().ValidateInvoice(dt);
            Assert.Single(result.Rows);
            Assert.Contains("Mismatch", result.Rows[0]["Eff. Days Validation"].ToString());
        }

        [Fact]
        public void ValidateInvoice_PartnerDiscountError()
        {
            var dt = CreateTable();
            dt.Rows.Add("2024-01-01","2024-01-30","6","30","5","5","10","10");
            var result = new InvoiceValidationService().ValidateInvoice(dt);
            Assert.Single(result.Rows);
            Assert.Contains("PartnerDiscountPercentage", result.Rows[0]["Partner Discount Validation"].ToString());
        }

        [Fact]
        public void ValidateInvoice_PartnerDiscount205_Passes()
        {
            var dt = CreateTable();
            dt.Rows.Add("2024-01-01","2024-01-30","6","30","20.05","5","10","10");
            var result = new InvoiceValidationService().ValidateInvoice(dt);
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void ValidateInvoice_HierarchyError()
        {
            var dt = CreateTable();
            dt.Rows.Add("2024-01-01","2024-01-30","4","30","10","20","10","5");
            var result = new InvoiceValidationService().ValidateInvoice(dt);
            Assert.Single(result.Rows);
            Assert.Contains("less than", result.Rows[0]["Discount Hierarchy Check"].ToString());
        }

        [Fact]
        public void ValidateInvoice_PricingConsistencyError()
        {
            var dt = CreateTable();
            dt.Rows.Add("2024-01-01","2024-01-30","4","30","19","5","15","10");
            var result = new InvoiceValidationService().ValidateInvoice(dt);
            Assert.Single(result.Rows);
            Assert.Contains("greater", result.Rows[0]["Pricing Consistency Check"].ToString());
        }

        [Fact]
        public void ValidateInvoice_MultipleErrors()
        {
            var dt = CreateTable();
            dt.Rows.Add("2024-01-01","2024-01-31","6","10","5","20","15","10");
            var result = new InvoiceValidationService().ValidateInvoice(dt);
            Assert.Single(result.Rows);
            var row = result.Rows[0];
            Assert.NotEmpty(row["Eff. Days Validation"].ToString());
            Assert.NotEmpty(row["Partner Discount Validation"].ToString());
            Assert.NotEmpty(row["Discount Hierarchy Check"].ToString());
            Assert.NotEmpty(row["Pricing Consistency Check"].ToString());
        }

        [Fact]
        public void ValidateInvoice_MissingColumns_DoesNotThrow()
        {
            var dt = new DataTable();
            dt.Columns.Add("ChargeStartDate");
            dt.Columns.Add("ChargeEndDate");
            dt.Rows.Add("2024-01-01", "2024-01-30");
            var svc = new InvoiceValidationService();
            var result = svc.ValidateInvoice(dt);
            Assert.NotNull(result);
        }

        [Fact]
        public void ValidateInvoice_EmptyTable_ReturnsEmpty()
        {
            var dt = CreateTable();
            var result = new InvoiceValidationService().ValidateInvoice(dt);
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void DiscountHierarchyCheck_Skipped_WhenCustomerDiscount100()
        {
            var dt = CreateTable();
            dt.Rows.Add("2024-01-01","2024-01-30","4","30","10","100","10","0");
            var result = new InvoiceValidationService().ValidateInvoice(dt);
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void PriceConsistencyCheck_Skipped_WhenCustomerDiscount100()
        {
            var dt = CreateTable();
            dt.Rows.Add("2024-01-01","2024-01-30","4","30","10","100","5","0");
            var result = new InvoiceValidationService().ValidateInvoice(dt);
            Assert.Empty(result.Rows);
        }
    }
}
