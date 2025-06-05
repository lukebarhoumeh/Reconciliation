using System;
using System.IO;
using Xunit;

namespace Reconciliation.Tests
{
    public class CliIntegrationTests
    {
        [Fact]
        public void CliRun_ReturnsZero()
        {
            string left = Path.GetTempFileName();
            string right = Path.GetTempFileName();
            File.WriteAllText(left, "CustomerDomainName,ProductId,SkuId,ChargeType,TermAndBillingCycle\nA,1,1,Charge,Monthly");
            File.WriteAllText(right, "InternalReferenceId,SkuId,BillingCycle\nX,1,Monthly");
            int result = ProgramInvoker.Invoke(new[] { "--left", left, "--right", right });
            File.Delete(left);
            File.Delete(right);
            Assert.Equal(0, result);
        }
    }

    internal static class ProgramInvoker
    {
        public static int Invoke(string[] args)
        {
            var type = Type.GetType("Reconciliation.Cli.Program, Reconciliation.Cli")!;
            var method = type.GetMethod("Main", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            object? result = method.Invoke(null, new object[] { args });
            return (int)result!;
        }
    }
}
