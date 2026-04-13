using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class AgentExecutionTraceRecorderReproTests
{
    [Fact]
    public async Task RecordAsync_persists_prompt_repro_fields()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentExecutionTraceRecorder sut = CreateSut(repo);
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

    [Fact]
    public async Task RecordAsync_persists_token_counts_and_estimated_cost_when_enabled()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        IOptions<LlmCostEstimationOptions> opts = Options.Create(
            new LlmCostEstimationOptions
            {
                Enabled = true,
                InputUsdPerMillionTokens = 1m,
                OutputUsdPerMillionTokens = 2m,
            });
        AgentExecutionTraceRecorder sut = new(repo, new LlmCostEstimator(opts), opts);

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
            promptRepro: null,
            inputTokenCount: 1_000_000,
            outputTokenCount: 500_000);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.InputTokenCount.Should().Be(1_000_000);
        t.OutputTokenCount.Should().Be(500_000);
        t.EstimatedCostUsd.Should().Be(2m);
    }

    private static AgentExecutionTraceRecorder CreateSut(InMemoryAgentExecutionTraceRepository repo)
    {
        IOptions<LlmCostEstimationOptions> disabled = Options.Create(new LlmCostEstimationOptions { Enabled = false });

        return new AgentExecutionTraceRecorder(repo, new LlmCostEstimator(disabled), disabled);
    }
}
