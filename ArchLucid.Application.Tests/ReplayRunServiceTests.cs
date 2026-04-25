using System.Data;

using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application.Agents;
using ArchLucid.Application.Authority;
using ArchLucid.Application.Common;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests;

/// <summary>
/// <see cref="ReplayRunService"/> unit tests (commit vs dry-run, not-found).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ReplayRunServiceTests
{
    private static IActorContext UnitTestActor()
    {
        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("unit-test");

        return actor.Object;
    }

    private static Mock<IAuthorityCommittedManifestChainWriter> CreateAuthorityChainWriterMock()
    {
        Mock<IAuthorityCommittedManifestChainWriter> mock = new();
        mock.Setup(
                x => x.PersistCommittedChainAsync(
                    It.IsAny<ScopeContext>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<GoldenManifest>(),
                    It.IsAny<AuthorityChainKeying>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IDbConnection?>(),
                    It.IsAny<IDbTransaction?>()))
            .ReturnsAsync(
                (ScopeContext _, Guid _, string _, GoldenManifest _, AuthorityChainKeying k, DateTime _, bool _, CancellationToken _, IDbConnection? _, IDbTransaction? _) =>
                    new AuthorityManifestPersistResult(
                        k.ContextSnapshotId,
                        k.GraphSnapshotId,
                        k.FindingsSnapshotId,
                        k.DecisionTraceId,
                        k.ManifestId));

        return mock;
    }

    private static ScopeContext TestScope() => new()
    {
        TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    };

    [Fact]
    public async Task ReplayAsync_when_run_missing_throws_RunNotFoundException()
    {
        Mock<IAgentExecutorResolver> resolver = new();
        Mock<IDecisionEngineService> decision = new();
        Mock<IArchitectureRequestRepository> requestRepo = new();
        Mock<IRunDetailQueryService> detail = new();
        detail.Setup(x => x.GetRunDetailAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        Mock<IRunRepository> authorityRuns = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(p => p.GetCurrentScope()).Returns(TestScope());

        Mock<IAgentEvidencePackageRepository> evidenceRepo = new();

        ReplayRunService sut = new(
            resolver.Object,
            decision.Object,
            requestRepo.Object,
            detail.Object,
            authorityRuns.Object,
            scopeProvider.Object,
            CreateAuthorityChainWriterMock().Object,
            evidenceRepo.Object,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            Mock.Of<IAuditService>(),
            UnitTestActor(),
            NullLogger<ReplayRunService>.Instance);

        Func<Task> act = async () => await sut.ReplayAsync("missing", ExecutionModes.Current, false, null, CancellationToken.None);

        await act.Should().ThrowAsync<RunNotFoundException>();
        authorityRuns.Verify(
            r => r.SaveAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null),
            Times.Never);
    }

    [Fact]
    public async Task ReplayAsync_commitReplay_false_does_not_update_run_to_committed()
    {
        string originalRunId = Guid.NewGuid().ToString("N");
        string requestId = "req-rep-" + Guid.NewGuid().ToString("N");

        ArchitectureRun originalRun = new()
        {
            RunId = originalRunId,
            RequestId = requestId,
            Status = ArchitectureRunStatus.Committed,
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v1",
        };

        AgentTask task = new()
        {
            TaskId = "t1",
            RunId = originalRunId,
            AgentType = AgentType.Topology,
            Objective = "o",
            Status = AgentTaskStatus.Completed,
            CreatedUtc = DateTime.UtcNow,
            EvidenceBundleRef = "eb",
        };

        ArchitectureRunDetail detailDto = new()
        {
            Run = originalRun,
            Tasks = [task],
        };

        Mock<IRunDetailQueryService> detail = new();
        detail.Setup(x => x.GetRunDetailAsync(originalRunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailDto);

        ArchitectureRequest request = new()
        {
            RequestId = requestId,
            SystemName = "S",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Description = "d",
        };

        Mock<IArchitectureRequestRepository> requestRepo = new();
        requestRepo.Setup(x => x.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        AgentEvidencePackage evidence = new()
        {
            RunId = originalRunId,
            RequestId = requestId,
            SystemName = "S",
            Environment = "prod",
            CloudProvider = "Azure",
            Request = new RequestEvidence { Description = "d" },
        };

        Mock<IAgentEvidencePackageRepository> evidenceRepo = new();
        evidenceRepo.Setup(x => x.GetByRunIdAsync(originalRunId, It.IsAny<CancellationToken>())).ReturnsAsync(evidence);

        AgentResult result = new()
        {
            RunId = "will-be-replay",
            TaskId = task.TaskId,
            AgentType = AgentType.Topology,
            Confidence = 0.8,
            ResultId = "r1",
            CreatedUtc = DateTime.UtcNow,
        };

        Mock<IAgentExecutor> executor = new();
        executor.Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<ArchitectureRequest>(),
                It.IsAny<AgentEvidencePackage>(),
                It.IsAny<IReadOnlyList<AgentTask>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentResult> { result });

        Mock<IAgentExecutorResolver> resolver = new();
        resolver.Setup(x => x.Resolve(ExecutionModes.Current)).Returns(executor.Object);

        Mock<IDecisionEngineService> decision = new();

        Mock<IRunRepository> authorityRuns = new();
        authorityRuns.Setup(x => x.SaveAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);
        authorityRuns.Setup(x => x.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(p => p.GetCurrentScope()).Returns(TestScope());

        ReplayRunService sut = new(
            resolver.Object,
            decision.Object,
            requestRepo.Object,
            detail.Object,
            authorityRuns.Object,
            scopeProvider.Object,
            CreateAuthorityChainWriterMock().Object,
            evidenceRepo.Object,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            Mock.Of<IAuditService>(),
            UnitTestActor(),
            NullLogger<ReplayRunService>.Instance);

        ReplayRunResult output = await sut.ReplayAsync(originalRunId, ExecutionModes.Current, commitReplay: false, null, CancellationToken.None);

        output.OriginalRunId.Should().Be(originalRunId);
        decision.Verify(
            x => x.MergeResults(
                It.IsAny<string>(),
                It.IsAny<ArchitectureRequest>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<AgentResult>>(),
                It.IsAny<IReadOnlyCollection<AgentEvaluation>>(),
                It.IsAny<IReadOnlyCollection<DecisionNode>>(),
                It.IsAny<string?>()),
            Times.Never);
        Guid replayGuid = Guid.Parse(output.ReplayRunId);
        authorityRuns.Verify(
            r => r.SaveAsync(It.Is<RunRecord>(rr => rr.RunId == replayGuid && rr.ProjectId == "S"), It.IsAny<CancellationToken>(), null, null),
            Times.Once);
        authorityRuns.Verify(
            r => r.UpdateAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null),
            Times.Never);
    }

    [Fact]
    public async Task ReplayAsync_commitReplay_true_persists_manifest_and_saves_authority_run()
    {
        string originalRunId = Guid.NewGuid().ToString("N");
        string requestId = "req-rep2-" + Guid.NewGuid().ToString("N");

        ArchitectureRun originalRun = new()
        {
            RunId = originalRunId,
            RequestId = requestId,
            Status = ArchitectureRunStatus.Committed,
            CreatedUtc = DateTime.UtcNow,
            CurrentManifestVersion = "v1",
        };

        AgentTask task = new()
        {
            TaskId = "t1",
            RunId = originalRunId,
            AgentType = AgentType.Topology,
            Objective = "o",
            Status = AgentTaskStatus.Completed,
            CreatedUtc = DateTime.UtcNow,
            EvidenceBundleRef = "eb",
        };

        ArchitectureRunDetail detailDto = new()
        {
            Run = originalRun,
            Tasks = [task],
        };

        Mock<IRunDetailQueryService> detail = new();
        detail.Setup(x => x.GetRunDetailAsync(originalRunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailDto);

        ArchitectureRequest request = new()
        {
            RequestId = requestId,
            SystemName = "S",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Description = "d",
        };

        Mock<IArchitectureRequestRepository> requestRepo = new();
        requestRepo.Setup(x => x.GetByIdAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        AgentEvidencePackage evidence = new()
        {
            RunId = originalRunId,
            RequestId = requestId,
            SystemName = "S",
            Environment = "prod",
            CloudProvider = "Azure",
            Request = new RequestEvidence { Description = "d" },
        };

        Mock<IAgentEvidencePackageRepository> evidenceRepo = new();
        evidenceRepo.Setup(x => x.GetByRunIdAsync(originalRunId, It.IsAny<CancellationToken>())).ReturnsAsync(evidence);

        List<AgentResult> results =
        [
            new()
            {
                RunId = "replay",
                TaskId = task.TaskId,
                AgentType = AgentType.Topology,
                Confidence = 0.9,
                ResultId = "r1",
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        Mock<IAgentExecutor> executor = new();
        executor.Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<ArchitectureRequest>(),
                It.IsAny<AgentEvidencePackage>(),
                It.IsAny<IReadOnlyList<AgentTask>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        Mock<IAgentExecutorResolver> resolver = new();
        resolver.Setup(x => x.Resolve(ExecutionModes.Current)).Returns(executor.Object);

        GoldenManifest merged = new()
        {
            RunId = "replay-run",
            SystemName = "S",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata { ManifestVersion = "v-override" },
        };

        List<DecisionTrace> traces =
        [
            RunEventTrace.From(new RunEventTracePayload
            {
                TraceId = "tr1",
                RunId = "replay-run",
                EventType = "merge",
                EventDescription = "d",
            }),
        ];

        Mock<IDecisionEngineService> decision = new();
        decision
            .Setup(x => x.MergeResults(
                It.IsAny<string>(),
                It.IsAny<ArchitectureRequest>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<AgentResult>>(),
                It.IsAny<IReadOnlyList<AgentEvaluation>>(),
                It.IsAny<IReadOnlyList<DecisionNode>>(),
                It.IsAny<string?>()))
            .Returns(
                new DecisionMergeResult
                {
                    Manifest = merged,
                    DecisionTraces = traces,
                    Errors = [],
                });

        Mock<IRunRepository> authorityRuns = new();
        authorityRuns.Setup(x => x.SaveAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);
        authorityRuns.Setup(x => x.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(p => p.GetCurrentScope()).Returns(TestScope());

        Mock<IAuthorityCommittedManifestChainWriter> chainWriter = CreateAuthorityChainWriterMock();

        ReplayRunService sut = new(
            resolver.Object,
            decision.Object,
            requestRepo.Object,
            detail.Object,
            authorityRuns.Object,
            scopeProvider.Object,
            chainWriter.Object,
            evidenceRepo.Object,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            Mock.Of<IAuditService>(),
            UnitTestActor(),
            NullLogger<ReplayRunService>.Instance);

        ReplayRunResult output =
            await sut.ReplayAsync(originalRunId, ExecutionModes.Current, commitReplay: true, manifestVersionOverride: "v-override", CancellationToken.None);

        output.Manifest.Should().NotBeNull();
        output.Manifest!.Metadata.ManifestVersion.Should().Be("v-override");
        // ADR 0030 PR A3 (2026-04-24): the legacy ICoordinatorDecisionTraceRepository second-write was removed;
        // decision traces are now persisted exclusively through the authority FK chain writer below.
        chainWriter.Verify(
            x => x.PersistCommittedChainAsync(
                It.IsAny<ScopeContext>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.Is<GoldenManifest>(m => m.Metadata.ManifestVersion == "v-override"),
                It.IsAny<AuthorityChainKeying>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IDbConnection?>(),
                It.IsAny<IDbTransaction?>()),
            Times.Once);
        Guid replayGuid = Guid.Parse(output.ReplayRunId);
        authorityRuns.Verify(
            r => r.SaveAsync(It.Is<RunRecord>(rr => rr.RunId == replayGuid), It.IsAny<CancellationToken>(), null, null),
            Times.Once);
        authorityRuns.Verify(
            r => r.UpdateAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null),
            Times.Never);
    }
}
