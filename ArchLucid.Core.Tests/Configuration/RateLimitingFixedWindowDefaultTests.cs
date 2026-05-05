using ArchLucid.Host.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Core.Tests.Configuration;

[Trait("Category", "Unit")]
public sealed class RateLimitingFixedWindowDefaultTests
{
    [Fact]
    public void RateLimitingDefaults_FixedWindowPermitLimit_is_100()
    {
        int expected = 100;

        RateLimitingDefaults.FixedWindowPermitLimit.Should().Be(expected);
    }

    [Fact]
    public void Configuration_GetValue_for_fixed_window_permit_matches_product_default_when_key_absent()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        int permit = configuration.GetValue(
            "RateLimiting:FixedWindow:PermitLimit",
            RateLimitingDefaults.FixedWindowPermitLimit);

        permit.Should().Be(100);
    }

    [Fact]
    public void RateLimitingDefaults_GovernancePolicyPackDryRunPermitLimit_is_12()
    {
        RateLimitingDefaults.GovernancePolicyPackDryRunPermitLimit.Should().Be(12);
    }
}
