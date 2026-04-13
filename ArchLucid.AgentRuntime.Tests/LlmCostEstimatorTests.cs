using FluentAssertions;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class LlmCostEstimatorTests
{
    [Fact]
    public void EstimateUsd_returns_null_when_disabled()
    {
        LlmCostEstimator sut = new(Options.Create(new LlmCostEstimationOptions { Enabled = false }));

        sut.EstimateUsd(100, 100).Should().BeNull();
    }

    [Fact]
    public void EstimateUsd_computes_when_enabled()
    {
        LlmCostEstimator sut = new(
            Options.Create(
                new LlmCostEstimationOptions
                {
                    Enabled = true,
                    InputUsdPerMillionTokens = 3m,
                    OutputUsdPerMillionTokens = 15m,
                }));

        decimal? usd = sut.EstimateUsd(2_000_000, 1_000_000);

        usd.Should().Be(21m);
    }
}
