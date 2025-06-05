using System.Diagnostics;
using System.IO;
using Xunit;

namespace Reconciliation.Tests
{
    public class CheckTestResultsScriptTests
    {
        private static string ScriptPath => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../..", "scripts", "check_test_results.sh"));

        [Fact]
        public void Script_ReturnsTrue_WhenDirHasFiles()
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "dummy.txt"), "x");
            string output = RunScript(dir);
            Assert.Contains("upload_artifact::true", output);
            Directory.Delete(dir, true);
        }

        [Fact]
        public void Script_ReturnsFalse_WhenDirMissing()
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string output = RunScript(dir);
            Assert.Contains("upload_artifact::false", output);
        }

        private static string RunScript(string dir)
        {
            var psi = new ProcessStartInfo("bash", $"{ScriptPath} {dir}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var process = Process.Start(psi)!;
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
    }
}
