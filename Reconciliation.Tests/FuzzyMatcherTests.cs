using Reconciliation;
using System.Collections.Generic;

namespace Reconciliation.Tests
{
    public class FuzzyMatcherTests
    {
        [Fact]
        public void FindClosest_ReturnsExactMatch()
        {
            var options = new List<string> { "SubscriptionId", "CustomerName" };
            var result = FuzzyMatcher.FindClosest("SubscriptionId", options);
            Assert.Equal("SubscriptionId", result);
        }

        [Fact]
        public void FindClosest_AllowsMinorTypos()
        {
            var options = new List<string> { "SubscriptionId", "CustomerName" };
            var result = FuzzyMatcher.FindClosest("subscription id", options);
            Assert.Equal("SubscriptionId", result);
        }
    }
}
