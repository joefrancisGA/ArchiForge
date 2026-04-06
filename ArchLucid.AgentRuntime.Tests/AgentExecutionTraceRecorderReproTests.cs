using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class AgentExecutionTraceRecorderReproTests
{
    [Fact]
    public async Task RecordAsync_persists_prompt_repro_fields()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentExecutionTraceRecorder sut = new(repo);
        AgentPromptReproMetadata meta = new("topology-system", "1.0.0", "abc123deadbeef", "pilot-a");

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "system",
            "user",
            "{}",
            "{}",
            parseSucceeded: true,
            errorMessage: null,
            meta);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");

        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.PromptTemplateId.Should().Be("topology-system");
        t.PromptTemplateVersion.Should().Be("1.0.0");
        t.SystemPromptContentSha256.Should().Be("abc123deadbeef");
        t.PromptReleaseLabel.Should().Be("pilot-a");
    }
}
