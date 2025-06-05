using Reconciliation;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace Reconciliation.Tests
{
    public class PerformanceTests
    {
        [Fact]
        public void StreamingCsv_Performance()
        {
            var temp = Path.GetTempFileName();
            try
            {
                using (var sw = new StreamWriter(temp))
                {
                    sw.WriteLine("Id,Quantity");
                    for (int i = 0; i < 1000; i++)
                    {
                        sw.WriteLine($"{i},1");
                    }
                }

                var watch = Stopwatch.StartNew();
                int count = CsvNormalizer.StreamCsv(temp).Count();
                watch.Stop();
                Assert.Equal(1000, count);
                // Report timing to output
                Debug.WriteLine($"Streaming read ms: {watch.ElapsedMilliseconds}");
            }
            finally
            {
                File.Delete(temp);
            }
        }
    }
}
