using System;
using System.IO;
using System.Linq;
#if HAS_UI

namespace Reconciliation.Tests
{
    public class BuildAsWinExeTest
    {
        [Fact]
        public void ReconciliationCompilesToExe()
        {
            var debugDir = Path.Combine("..", "Reconciliation", "bin", "Debug");
            var exePath = Directory.GetFiles(debugDir, "Reconciliation.exe", SearchOption.AllDirectories).FirstOrDefault();
            Assert.True(File.Exists(exePath), $"Executable not found in {debugDir}");
        }
    }
}
#endif
