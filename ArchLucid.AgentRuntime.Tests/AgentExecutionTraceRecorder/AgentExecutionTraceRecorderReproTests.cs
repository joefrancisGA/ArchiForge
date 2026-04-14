using ArchLucid.AgentRuntime;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.BlobStore;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class AgentExecutionTraceRecorderReproTests
{
    [Fact]
    public async Task RecordAsync_persists_prompt_repro_fields()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentExecutionTraceRecorder sut = CreateRecorder(repo, persistFull: false);

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
    public async Task RecordAsync_when_model_metadata_null_uses_unspecified_sentinels()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentExecutionTraceRecorder sut = CreateRecorder(repo, persistFull: false);

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
            inputTokenCount: null,
            outputTokenCount: null,
            modelDeploymentName: null,
            modelVersion: null);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.ModelDeploymentName.Should().Be(AgentExecutionTraceModelMetadata.UnspecifiedDeploymentName);
        t.ModelVersion.Should().Be(AgentExecutionTraceModelMetadata.UnspecifiedModelVersion);
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
        AgentExecutionTraceRecorder sut = CreateRecorder(repo, persistFull: false, costOptions: opts);

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

    [Fact]
    public async Task RecordAsync_when_persist_full_true_sets_blob_keys_after_background_upload()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentExecutionTraceRecorder sut = CreateRecorder(repo, persistFull: true);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "full-system",
            "full-user",
            "full-response",
            "{}",
            parseSucceeded: true,
            errorMessage: null);

        await Task.Delay(500);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.FullSystemPromptBlobKey.Should().NotBeNullOrEmpty();
        t.FullUserPromptBlobKey.Should().NotBeNullOrEmpty();
        t.FullResponseBlobKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RecordAsync_when_persist_full_false_does_not_set_blob_keys()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentExecutionTraceRecorder sut = CreateRecorder(repo, persistFull: false);

        await sut.RecordAsync(
            "run-1",
            "task-1",
            AgentType.Topology,
            "full-system",
            "full-user",
            "full-response",
            "{}",
            parseSucceeded: true,
            errorMessage: null);

        await Task.Delay(500);

        IReadOnlyList<AgentExecutionTrace> list = await repo.GetByRunIdAsync("run-1");
        AgentExecutionTrace t = list.Should().ContainSingle().Subject;
        t.FullSystemPromptBlobKey.Should().BeNull();
        t.FullUserPromptBlobKey.Should().BeNull();
        t.FullResponseBlobKey.Should().BeNull();
    }

    private static AgentExecutionTraceRecorder CreateRecorder(
        InMemoryAgentExecutionTraceRepository repo,
        bool persistFull,
        IOptions<LlmCostEstimationOptions>? costOptions = null)
    {
        IOptions<LlmCostEstimationOptions> cost = costOptions ?? Options.Create(new LlmCostEstimationOptions { Enabled = false });
        ServiceCollection services = new();
        services.AddScoped<IAgentExecutionTraceRepository>(_ => repo);
        services.AddSingleton<IArtifactBlobStore, InMemoryArtifactBlobStore>();
        services.AddSingleton(cost);
        services.AddSingleton(
            Options.Create(new AgentExecutionTraceStorageOptions { PersistFullPrompts = persistFull }));
        services.AddSingleton<ILlmCostEstimator, LlmCostEstimator>();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.None));
        services.AddScoped<AgentExecutionTraceRecorder>();
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScope scope = provider.CreateScope();

        return scope.ServiceProvider.GetRequiredService<AgentExecutionTraceRecorder>();
    }
}
