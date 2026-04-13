using ArchLucid.Application.Architecture;
using ArchLucid.Application.Common;
using ArchLucid.Application.Governance;
using ArchLucid.Application.Runs;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Governance.Resolution;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Decisioning.Repositories;
using ArchLucid.Decisioning.Validation;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Governance;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Repositories;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests;

/// <summary>
/// Commit orchestration with real <see cref="DecisionEngineService"/> and in-memory coordinator repositories.
/// Catches divergence between mocked merge output and production merge/trace attachment (e.g. traceability gaps).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class ArchitectureRunCommitPipelineIntegrationTests
{
    [Fact]
    public async Task CommitRunAsync_real_merge_satisfies_traceability_and_persists_consistent_rows()
    {
        InMemoryArchitectureRequestRepository requestRepository = new();
        InMemoryRunRepository authorityRunRepository = new();
        ScopeContext authorityScope = new()
        {
            TenantId = Guid.Parse("10101010-1010-1010-1010-101010101010"),
            WorkspaceId = Guid.Parse("20202020-2020-2020-2020-202020202020"),
            ProjectId = Guid.Parse("30303030-3030-3030-3030-303030303030"),
        };
        Mock<IScopeContextProvider> scopeContextProvider = new();
        scopeContextProvider.Setup(s => s.GetCurrentScope()).Returns(authorityScope);
        InMemoryAgentTaskRepository taskRepository = new();
        InMemoryAgentResultRepository resultRepository = new();
        InMemoryAgentEvaluationRepository evaluationRepository = new();
        InMemoryAgentEvidencePackageRepository evidencePackageRepository = new();
        InMemoryDecisionNodeRepository decisionNodeRepository = new();
        InMemoryCoordinatorGoldenManifestRepository manifestRepository = new();
        InMemoryCoordinatorDecisionTraceRepository traceRepository = new();

        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-commit-pipe-" + runId;

        ArchitectureRequest request = new()
        {
            RequestId = requestId,
            SystemName = "PipelineSys",
            Description = "Integration description long enough for validation rules.",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
        };

        await requestRepository.CreateAsync(request, CancellationToken.None);

        DateTime createdUtc = DateTime.UtcNow;
        Guid authorityRunGuid = Guid.ParseExact(runId, "N");
        await authorityRunRepository.SaveAsync(
            new RunRecord
            {
                RunId = authorityRunGuid,
                TenantId = authorityScope.TenantId,
                WorkspaceId = authorityScope.WorkspaceId,
                ScopeProjectId = authorityScope.ProjectId,
                ProjectId = request.SystemName,
                ArchitectureRequestId = requestId,
                LegacyRunStatus = ArchitectureRunStatus.ReadyForCommit.ToString(),
                CreatedUtc = createdUtc,
            },
            CancellationToken.None);

        string taskId = "task-topo-" + runId;

        AgentTask task = new()
        {
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Objective = "Evaluate topology",
            Status = AgentTaskStatus.Completed,
            CreatedUtc = DateTime.UtcNow,
        };

        await taskRepository.CreateManyAsync([task], CancellationToken.None);

        AgentResult topologyResult = new()
        {
            ResultId = "res-topo-" + runId,
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Confidence = 0.92,
            Claims = ["topology-ok"],
            EvidenceRefs = ["ev-1"],
            CreatedUtc = DateTime.UtcNow,
        };

        await resultRepository.CreateManyAsync([topologyResult], CancellationToken.None);
        await evaluationRepository.CreateManyAsync([], CancellationToken.None);

        AgentEvidencePackage evidence = new()
        {
            RunId = runId,
            RequestId = requestId,
            SystemName = request.SystemName,
            Environment = request.Environment,
            CloudProvider = request.CloudProvider.ToString(),
        };

        await evidencePackageRepository.CreateAsync(evidence, CancellationToken.None);

        DecisionEngineService decisionEngine = new(new PassthroughSchemaValidationService());
        DecisionEngineV2 decisionEngineV2 = new();

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("integration-test");

        ArchitectureRunCommitOrchestrator commitOrchestrator = new(
            authorityRunRepository,
            scopeContextProvider.Object,
            requestRepository,
            taskRepository,
            resultRepository,
            evaluationRepository,
            evidencePackageRepository,
            decisionEngine,
            decisionEngineV2,
            decisionNodeRepository,
            manifestRepository,
            traceRepository,
            actor.Object,
            Mock.Of<IBaselineMutationAuditService>(),
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            Mock.Of<IPreCommitGovernanceGate>(),
            Options.Create(new PreCommitGovernanceGateOptions()),
            Mock.Of<IAuditService>(),
            NullLogger<ArchitectureRunCommitOrchestrator>.Instance);

        CommitRunResult committed = await commitOrchestrator.CommitRunAsync(runId, CancellationToken.None);

        CommitRunResult idempotentCommit = await commitOrchestrator.CommitRunAsync(runId, CancellationToken.None);
        idempotentCommit.Manifest.Metadata.ManifestVersion.Should().Be(committed.Manifest.Metadata.ManifestVersion);

        committed.Manifest.RunId.Should().Be(runId);
        committed.DecisionTraces.Should().NotBeEmpty();

        CommittedManifestTraceabilityRules
            .GetLinkageGaps(committed.Manifest, committed.DecisionTraces)
            .Should()
            .BeEmpty();

        string version = committed.Manifest.Metadata.ManifestVersion;
        version.Should().NotBeNullOrWhiteSpace();

        GoldenManifest? storedManifest = await manifestRepository.GetByVersionAsync(version, CancellationToken.None);
        storedManifest.Should().NotBeNull();

        IReadOnlyList<DecisionTrace> storedTraces =
            await traceRepository.GetByRunIdAsync(runId, CancellationToken.None);

        storedTraces.Should().NotBeEmpty();

        CommittedManifestTraceabilityRules
            .GetLinkageGaps(storedManifest, storedTraces)
            .Should()
            .BeEmpty();

        RunRecord? authorityHeader =
            await authorityRunRepository.GetByIdAsync(authorityScope, authorityRunGuid, CancellationToken.None);
        authorityHeader.Should().NotBeNull();
        authorityHeader!.LegacyRunStatus.Should().Be(ArchitectureRunStatus.Committed.ToString());
        authorityHeader.CurrentManifestVersion.Should().Be(version);
        authorityHeader.CompletedUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task CommitRunAsync_blocked_by_governance_gate_throws_and_does_not_persist_manifest()
    {
        InMemoryArchitectureRequestRepository requestRepository = new();
        InMemoryRunRepository authorityRunRepository = new();
        InMemoryFindingsSnapshotRepository findingsRepository = new();
        InMemoryPolicyPackAssignmentRepository assignmentRepository = new();
        ScopeContext authorityScope = new()
        {
            TenantId = Guid.Parse("40404040-4040-4040-4040-404040404040"),
            WorkspaceId = Guid.Parse("50505050-5050-5050-5050-505050505050"),
            ProjectId = Guid.Parse("60606060-6060-6060-6060-606060606060"),
        };
        Mock<IScopeContextProvider> scopeContextProvider = new();
        scopeContextProvider.Setup(s => s.GetCurrentScope()).Returns(authorityScope);
        InMemoryAgentTaskRepository taskRepository = new();
        InMemoryAgentResultRepository resultRepository = new();
        InMemoryAgentEvaluationRepository evaluationRepository = new();
        InMemoryAgentEvidencePackageRepository evidencePackageRepository = new();
        InMemoryDecisionNodeRepository decisionNodeRepository = new();
        InMemoryCoordinatorGoldenManifestRepository manifestRepository = new();
        InMemoryCoordinatorDecisionTraceRepository traceRepository = new();

        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-gate-block-" + runId;
        Guid authorityRunGuid = Guid.ParseExact(runId, "N");
        Guid snapshotId = Guid.NewGuid();
        Guid packId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        ArchitectureRequest request = new()
        {
            RequestId = requestId,
            SystemName = "GateBlockSys",
            Description = "Integration description long enough for validation rules.",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
        };

        await requestRepository.CreateAsync(request, CancellationToken.None);

        await authorityRunRepository.SaveAsync(
            new RunRecord
            {
                RunId = authorityRunGuid,
                TenantId = authorityScope.TenantId,
                WorkspaceId = authorityScope.WorkspaceId,
                ScopeProjectId = authorityScope.ProjectId,
                ProjectId = request.SystemName,
                ArchitectureRequestId = requestId,
                LegacyRunStatus = ArchitectureRunStatus.ReadyForCommit.ToString(),
                CreatedUtc = DateTime.UtcNow,
                FindingsSnapshotId = snapshotId,
            },
            CancellationToken.None);

        await findingsRepository.SaveAsync(
            new ArchLucid.Decisioning.Models.FindingsSnapshot
            {
                FindingsSnapshotId = snapshotId,
                RunId = authorityRunGuid,
                ContextSnapshotId = Guid.NewGuid(),
                GraphSnapshotId = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow,
                Findings =
                [
                    new ArchLucid.Decisioning.Models.Finding
                    {
                        FindingId = "f-critical-block",
                        FindingType = "Compliance",
                        Category = "c",
                        EngineType = "e",
                        Severity = ArchLucid.Decisioning.Models.FindingSeverity.Critical,
                        Title = "t",
                        Rationale = "r",
                    },
                ],
            },
            CancellationToken.None);

        await assignmentRepository.CreateAsync(
            new PolicyPackAssignment
            {
                TenantId = authorityScope.TenantId,
                WorkspaceId = authorityScope.WorkspaceId,
                ProjectId = authorityScope.ProjectId,
                ScopeLevel = GovernanceScopeLevel.Project,
                PolicyPackId = packId,
                PolicyPackVersion = "1.0.0",
                IsEnabled = true,
                BlockCommitOnCritical = true,
            },
            CancellationToken.None);

        string taskId = "task-topo-gate-" + runId;
        AgentTask task = new()
        {
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Objective = "Evaluate topology",
            Status = AgentTaskStatus.Completed,
            CreatedUtc = DateTime.UtcNow,
        };

        await taskRepository.CreateManyAsync([task], CancellationToken.None);

        AgentResult topologyResult = new()
        {
            ResultId = "res-topo-gate-" + runId,
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Confidence = 0.92,
            Claims = ["topology-ok"],
            EvidenceRefs = ["ev-1"],
            CreatedUtc = DateTime.UtcNow,
        };

        await resultRepository.CreateManyAsync([topologyResult], CancellationToken.None);
        await evaluationRepository.CreateManyAsync([], CancellationToken.None);

        AgentEvidencePackage evidence = new()
        {
            RunId = runId,
            RequestId = requestId,
            SystemName = request.SystemName,
            Environment = request.Environment,
            CloudProvider = request.CloudProvider.ToString(),
        };

        await evidencePackageRepository.CreateAsync(evidence, CancellationToken.None);

        DecisionEngineService decisionEngine = new(new PassthroughSchemaValidationService());
        DecisionEngineV2 decisionEngineV2 = new();

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("integration-gate-test");

        Mock<IAuditService> auditService = new();

        PreCommitGovernanceGate gate = new(
            Options.Create(new PreCommitGovernanceGateOptions { PreCommitGateEnabled = true }),
            scopeContextProvider.Object,
            authorityRunRepository,
            findingsRepository,
            assignmentRepository);

        ArchitectureRunCommitOrchestrator commitOrchestrator = new(
            authorityRunRepository,
            scopeContextProvider.Object,
            requestRepository,
            taskRepository,
            resultRepository,
            evaluationRepository,
            evidencePackageRepository,
            decisionEngine,
            decisionEngineV2,
            decisionNodeRepository,
            manifestRepository,
            traceRepository,
            actor.Object,
            Mock.Of<IBaselineMutationAuditService>(),
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            gate,
            Options.Create(new PreCommitGovernanceGateOptions { PreCommitGateEnabled = true }),
            auditService.Object,
            NullLogger<ArchitectureRunCommitOrchestrator>.Instance);

        Func<Task> act = async () => await commitOrchestrator.CommitRunAsync(runId, CancellationToken.None);

        await act.Should().ThrowAsync<PreCommitGovernanceBlockedException>();

        string expectedVersion = "v1-" + runId;
        GoldenManifest? stored = await manifestRepository.GetByVersionAsync(expectedVersion, CancellationToken.None);
        stored.Should().BeNull();

        auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.GovernancePreCommitBlocked && e.RunId == authorityRunGuid),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitRunAsync_governance_gate_allows_when_no_critical_findings_and_commits_successfully()
    {
        InMemoryArchitectureRequestRepository requestRepository = new();
        InMemoryRunRepository authorityRunRepository = new();
        InMemoryFindingsSnapshotRepository findingsRepository = new();
        InMemoryPolicyPackAssignmentRepository assignmentRepository = new();
        ScopeContext authorityScope = new()
        {
            TenantId = Guid.Parse("70707070-7070-7070-7070-707070707070"),
            WorkspaceId = Guid.Parse("80808080-8080-8080-8080-808080808080"),
            ProjectId = Guid.Parse("90909090-9090-9090-9090-909090909090"),
        };
        Mock<IScopeContextProvider> scopeContextProvider = new();
        scopeContextProvider.Setup(s => s.GetCurrentScope()).Returns(authorityScope);
        InMemoryAgentTaskRepository taskRepository = new();
        InMemoryAgentResultRepository resultRepository = new();
        InMemoryAgentEvaluationRepository evaluationRepository = new();
        InMemoryAgentEvidencePackageRepository evidencePackageRepository = new();
        InMemoryDecisionNodeRepository decisionNodeRepository = new();
        InMemoryCoordinatorGoldenManifestRepository manifestRepository = new();
        InMemoryCoordinatorDecisionTraceRepository traceRepository = new();

        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-gate-allow-int-" + runId;
        Guid authorityRunGuid = Guid.ParseExact(runId, "N");
        Guid snapshotId = Guid.NewGuid();

        ArchitectureRequest request = new()
        {
            RequestId = requestId,
            SystemName = "GateAllowSys",
            Description = "Integration description long enough for validation rules.",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
        };

        await requestRepository.CreateAsync(request, CancellationToken.None);

        await authorityRunRepository.SaveAsync(
            new RunRecord
            {
                RunId = authorityRunGuid,
                TenantId = authorityScope.TenantId,
                WorkspaceId = authorityScope.WorkspaceId,
                ScopeProjectId = authorityScope.ProjectId,
                ProjectId = request.SystemName,
                ArchitectureRequestId = requestId,
                LegacyRunStatus = ArchitectureRunStatus.ReadyForCommit.ToString(),
                CreatedUtc = DateTime.UtcNow,
                FindingsSnapshotId = snapshotId,
            },
            CancellationToken.None);

        await findingsRepository.SaveAsync(
            new ArchLucid.Decisioning.Models.FindingsSnapshot
            {
                FindingsSnapshotId = snapshotId,
                RunId = authorityRunGuid,
                ContextSnapshotId = Guid.NewGuid(),
                GraphSnapshotId = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow,
                Findings =
                [
                    new ArchLucid.Decisioning.Models.Finding
                    {
                        FindingId = "f-warn-only",
                        FindingType = "Compliance",
                        Category = "c",
                        EngineType = "e",
                        Severity = ArchLucid.Decisioning.Models.FindingSeverity.Warning,
                        Title = "t",
                        Rationale = "r",
                    },
                ],
            },
            CancellationToken.None);

        await assignmentRepository.CreateAsync(
            new PolicyPackAssignment
            {
                TenantId = authorityScope.TenantId,
                WorkspaceId = authorityScope.WorkspaceId,
                ProjectId = authorityScope.ProjectId,
                ScopeLevel = GovernanceScopeLevel.Project,
                PolicyPackId = Guid.NewGuid(),
                PolicyPackVersion = "1.0.0",
                IsEnabled = true,
                BlockCommitOnCritical = true,
            },
            CancellationToken.None);

        string taskId = "task-topo-allow-" + runId;
        AgentTask task = new()
        {
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Objective = "Evaluate topology",
            Status = AgentTaskStatus.Completed,
            CreatedUtc = DateTime.UtcNow,
        };

        await taskRepository.CreateManyAsync([task], CancellationToken.None);

        AgentResult topologyResult = new()
        {
            ResultId = "res-topo-allow-" + runId,
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Confidence = 0.92,
            Claims = ["topology-ok"],
            EvidenceRefs = ["ev-1"],
            CreatedUtc = DateTime.UtcNow,
        };

        await resultRepository.CreateManyAsync([topologyResult], CancellationToken.None);
        await evaluationRepository.CreateManyAsync([], CancellationToken.None);

        AgentEvidencePackage evidence = new()
        {
            RunId = runId,
            RequestId = requestId,
            SystemName = request.SystemName,
            Environment = request.Environment,
            CloudProvider = request.CloudProvider.ToString(),
        };

        await evidencePackageRepository.CreateAsync(evidence, CancellationToken.None);

        DecisionEngineService decisionEngine = new(new PassthroughSchemaValidationService());
        DecisionEngineV2 decisionEngineV2 = new();

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("integration-gate-allow");

        PreCommitGovernanceGate gate = new(
            Options.Create(new PreCommitGovernanceGateOptions { PreCommitGateEnabled = true }),
            scopeContextProvider.Object,
            authorityRunRepository,
            findingsRepository,
            assignmentRepository);

        ArchitectureRunCommitOrchestrator commitOrchestrator = new(
            authorityRunRepository,
            scopeContextProvider.Object,
            requestRepository,
            taskRepository,
            resultRepository,
            evaluationRepository,
            evidencePackageRepository,
            decisionEngine,
            decisionEngineV2,
            decisionNodeRepository,
            manifestRepository,
            traceRepository,
            actor.Object,
            Mock.Of<IBaselineMutationAuditService>(),
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            gate,
            Options.Create(new PreCommitGovernanceGateOptions { PreCommitGateEnabled = true }),
            Mock.Of<IAuditService>(),
            NullLogger<ArchitectureRunCommitOrchestrator>.Instance);

        CommitRunResult committed = await commitOrchestrator.CommitRunAsync(runId, CancellationToken.None);

        committed.Manifest.Should().NotBeNull();
        string version = committed.Manifest.Metadata.ManifestVersion;
        GoldenManifest? storedManifest = await manifestRepository.GetByVersionAsync(version, CancellationToken.None);
        storedManifest.Should().NotBeNull();
    }
}
