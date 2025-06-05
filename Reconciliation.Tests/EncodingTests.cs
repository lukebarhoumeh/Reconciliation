using Reconciliation;
using System.Data;
using System.IO;
using System.Text;
using Xunit;

namespace Reconciliation.Tests
{
    public class EncodingTests
    {
        [Fact]
        public void NormalizeCsv_IgnoresBlankLinesAndBom()
        {
            var temp = Path.GetTempFileName();
            try
            {
                var bom = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                File.WriteAllText(temp, bom + "Id,Quantity\n\n1,2\n\n");
                var view = CsvNormalizer.NormalizeCsv(temp);
                Assert.Equal(1, view.Table.Rows.Count);
            }
            finally
            {
                File.Delete(temp);
            }
        }
    }
}
