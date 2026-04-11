using ArchLucid.Application.Architecture;
using ArchLucid.Application.Common;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Decisioning.Validation;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

#pragma warning disable CS0618 // RunsAuthorityConvergence: tracked for migration by 2026-09-30 — integration fixture uses InMemoryArchitectureRunRepository (TreatWarningsAsErrors).

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
        InMemoryArchitectureRunRepository runRepository = new(requestRepository);
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

        ArchitectureRun run = new()
        {
            RunId = runId,
            RequestId = requestId,
            Status = ArchitectureRunStatus.ReadyForCommit,
            CreatedUtc = DateTime.UtcNow,
        };

        await runRepository.CreateAsync(run, CancellationToken.None);

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
            runRepository,
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
            NullLogger<ArchitectureRunCommitOrchestrator>.Instance);

        CommitRunResult committed = await commitOrchestrator.CommitRunAsync(runId, CancellationToken.None);

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

        ArchitectureRun? updatedRun = await runRepository.GetByIdAsync(runId, CancellationToken.None);
        updatedRun.Should().NotBeNull();
        updatedRun.Status.Should().Be(ArchitectureRunStatus.Committed);
        updatedRun.CurrentManifestVersion.Should().Be(version);
    }
}

#pragma warning restore CS0618
