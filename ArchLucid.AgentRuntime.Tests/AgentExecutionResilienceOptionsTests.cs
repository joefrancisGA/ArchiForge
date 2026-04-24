using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AgentExecutionResilienceOptionsTests
{
    [Fact]
    public void Normalize_ClampsNegativeRetryAttempts_ToZero()
    {
        AgentExecutionResilienceOptions o = new() { LlmCallMaxRetryAttempts = -5 };
        o.Normalize();

        o.LlmCallMaxRetryAttempts.Should().Be(0);
    }

    [Fact]
    public void Normalize_ClampsExcessiveBaseDelay_To30s()
    {
        AgentExecutionResilienceOptions o = new() { LlmCallBaseDelayMilliseconds = 999_999 };
        o.Normalize();

        o.LlmCallBaseDelayMilliseconds.Should().Be(30_000);
    }

    [Fact]
    public void Normalize_ClampsMaxDelayTo120s()
    {
        AgentExecutionResilienceOptions o = new() { LlmCallMaxDelaySeconds = 999 };
        o.Normalize();

        o.LlmCallMaxDelaySeconds.Should().Be(120);
    }

    [Fact]
    public void Defaults_AreReasonable()
    {
        AgentExecutionResilienceOptions o = new();
        o.Normalize();

        o.LlmCallMaxRetryAttempts.Should().Be(3);
        o.LlmCallBaseDelayMilliseconds.Should().Be(500);
        o.LlmCallMaxDelaySeconds.Should().Be(10);
    }
}
