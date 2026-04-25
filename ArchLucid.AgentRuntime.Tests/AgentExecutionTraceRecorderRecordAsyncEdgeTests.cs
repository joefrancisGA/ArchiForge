using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Llm.Redaction;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.BlobStore;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;
// Child namespace `ArchLucid.AgentRuntime.Tests.AgentExecutionTraceRecorder` shadows the runtime type name in this parent namespace (CS0118).
using AgentExecutionTraceRecorderImpl = ArchLucid.AgentRuntime.AgentExecutionTraceRecorder;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
///     Branch coverage for <see cref="ArchLucid.AgentRuntime.AgentExecutionTraceRecorder.RecordAsync" /> (validation, cost
///     off, simulator short-circuit).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AgentExecutionTraceRecorderRecordAsyncEdgeTests
{
    [Fact]
    public async Task RecordAsync_throws_when_run_id_whitespace()
    {
        AgentExecutionTraceRecorderImpl sut = CreateSut(false);

        Func<Task> act = async () => await sut.RecordAsync(
            "   ",
            "task-1",
            AgentType.Topology,
            "sys",
            "user",
            "{}",
            "{}",
            true,
            null,
            cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RecordAsync_throws_when_task_id_empty()
    {
        AgentExecutionTraceRecorderImpl sut = CreateSut(false);

        Func<Task> act = async () => await sut.RecordAsync(
            Guid.NewGuid().ToString("N"),
            "",
            AgentType.Topology,
            "sys",
            "user",
            "{}",
            "{}",
            true,
            null,
            cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RecordAsync_when_cost_disabled_does_not_set_estimated_cost_despite_token_counts()
    {
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentExecutionTraceRecorderImpl sut = CreateSut(repo, false);
        string runId = Guid.NewGuid().ToString("N");

        await sut.RecordAsync(
            runId,
            "task-cost-off",
            AgentType.Topology,
            "s",
            "u",
            "{}",
            "{}",
            true,
            null,
            inputTokenCount: 100,
            outputTokenCount: 50,
            isSimulatorExecution: true,
            cancellationToken: CancellationToken.None);

        IReadOnlyList<AgentExecutionTrace> traces = await repo.GetByRunIdAsync(runId, CancellationToken.None);
        traces.Should().ContainSingle();
        traces[0].EstimatedCostUsd.Should().BeNull();
    }

    [Fact]
    public async Task RecordAsync_simulator_skips_blob_persistence_after_create()
    {
        Mock<IArtifactBlobStore> blobs = new();
        InMemoryAgentExecutionTraceRepository repo = new();
        AgentExecutionTraceRecorderImpl sut = CreateSut(repo, false, blobs.Object);

        await sut.RecordAsync(
            Guid.NewGuid().ToString("N"),
            "task-sim",
            AgentType.Topology,
            "s",
            "u",
            "{}",
            "{}",
            true,
            null,
            isSimulatorExecution: true,
            cancellationToken: CancellationToken.None);

        blobs.Verify(
            b => b.WriteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AgentExecutionTraceRecorderImpl CreateSut(bool costEnabled)
    {
        return CreateSut(new InMemoryAgentExecutionTraceRepository(), costEnabled, Mock.Of<IArtifactBlobStore>());
    }

    private static AgentExecutionTraceRecorderImpl CreateSut(
        InMemoryAgentExecutionTraceRepository repo,
        bool costEnabled,
        IArtifactBlobStore? blobStore = null)
    {
        Mock<ILlmCostEstimator> cost = new();
        cost.Setup(c => c.EstimateUsd(It.IsAny<int>(), It.IsAny<int>())).Returns(1.23m);

        Mock<IOptionsMonitor<LlmPromptRedactionOptions>> redactionMonitor = new();
        redactionMonitor.Setup(m => m.CurrentValue).Returns(new LlmPromptRedactionOptions { Enabled = false });

        IPromptRedactor redactor = new PromptRedactor(redactionMonitor.Object, NullLogger<PromptRedactor>.Instance);

        return new AgentExecutionTraceRecorderImpl(
            repo,
            cost.Object,
            Options.Create(new LlmCostEstimationOptions { Enabled = costEnabled }),
            Options.Create(new AgentExecutionTraceStorageOptions()),
            blobStore ?? Mock.Of<IArtifactBlobStore>(),
            new NoOpAuditService(),
            new FixedScopeProvider(),
            redactionMonitor.Object,
            redactor,
            NullLogger<AgentExecutionTraceRecorderImpl>.Instance);
    }

    private sealed class FixedScopeProvider : IScopeContextProvider
    {
        public ScopeContext GetCurrentScope()
        {
            return new ScopeContext
            {
                TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                WorkspaceId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                ProjectId = Guid.Parse("00000000-0000-0000-0000-000000000003")
            };
        }
    }

    private sealed class NoOpAuditService : IAuditService
    {
        public Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
