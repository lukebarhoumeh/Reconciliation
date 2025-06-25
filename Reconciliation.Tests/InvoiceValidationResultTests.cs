using System.Data;
using Reconciliation;
using Xunit;

namespace Reconciliation.Tests
{
    public class InvoiceValidationResultTests
    {
        [Fact]
        public void InvalidRowsView_ReturnsDataView()
        {
            var t = new DataTable();
            t.Columns.Add("A");
            var r = new InvoiceValidationResult(t, 0, 0);
            Assert.Equal(t.DefaultView, r.InvalidRowsView);
        }
    }
}
