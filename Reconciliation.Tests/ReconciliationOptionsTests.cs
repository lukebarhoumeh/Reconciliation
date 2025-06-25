using System.Text.Json;
using Reconciliation;

namespace Reconciliation.Tests;

public class ReconciliationOptionsTests
{
    [Fact]
    public void Default_HighPriorityThreshold_Is20()
    {
        Assert.Equal(20m, AppConfig.Reconciliation.HighPriorityThreshold);
    }

    [Fact]
    public void JsonOverride_RoundTrips()
    {
        const string json = "{ \"Reconciliation\": { \"highPriorityThreshold\": 5 } }";
        var opts = JsonSerializer.Deserialize<Wrapper>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(opts?.Reconciliation);
        Assert.Equal(5m, opts!.Reconciliation!.HighPriorityThreshold);
    }

    private sealed class Wrapper
    {
        public ReconciliationOptions? Reconciliation { get; set; }
    }
}
