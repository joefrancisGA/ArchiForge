using ArchLucid.AgentRuntime;
using ArchLucid.Contracts.Agents;
using ArchLucid.Core;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class AgentHandlerExecutionFailureReasonTests
{
    [Fact]
    public void ResolveFailureReasonCode_llm_quota()
    {
        LlmTokenQuotaExceededException ex = new("q", DateTimeOffset.UtcNow.AddHours(1));

        string? code = AgentHandlerExecutionFailureReason.ResolveFailureReasonCode(ex);

        code.Should().Be(AgentExecutionTraceFailureReasonCodes.LlmTokenQuotaExceeded);
    }
}
