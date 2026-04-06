using ArchiForge.AgentSimulator.Services;
using ArchiForge.Application.Common;
using ArchiForge.Application.Decisions;
using ArchiForge.Application.Evidence;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Contracts.Requests;
using ArchiForge.Coordinator.Services;
using ArchiForge.Persistence.Data.Repositories;
using ArchiForge.Decisioning.Merge;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchiForge.Application.Tests;

/// <summary>
/// <see cref="ArchitectureRunService.ExecuteRunAsync"/> and <see cref="ArchitectureRunService.CommitRunAsync"/> behavior with mocked persistence and engines.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ArchitectureRunServiceExecuteCommitTests
{
    [Fact]
    public async Task ExecuteRunAsync_happy_path_persists_results_and_marks_ready_for_commit()
    {
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-ex-" + Guid.NewGuid().ToString("N");
        Mock<IArchitectureRunRepository> runRepo = new();
        runRepo.Setup(x => x.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRun
                {
                    RunId = runId,
                    RequestId = requestId,
                    Status = ArchitectureRunStatus.TasksGenerated,
                    CreatedUtc = DateTime.UtcNow,
                });

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

        AgentTask task = new()
        {
            TaskId = "t-ex",
            RunId = runId,
            AgentType = AgentType.Topology,
            Objective = "o",
            Status = AgentTaskStatus.Created,
            CreatedUtc = DateTime.UtcNow,
            EvidenceBundleRef = "eb",
        };

        Mock<IAgentTaskRepository> taskRepo = new();
        taskRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentTask> { task });

        AgentEvidencePackage package = new()
        {
            RunId = runId,
            RequestId = requestId,
            SystemName = "S",
            Environment = "prod",
            CloudProvider = "Azure",
            Request = new RequestEvidence { Description = "d" },
        };

        Mock<IEvidenceBuilder> evidenceBuilder = new();
        evidenceBuilder.Setup(x => x.BuildAsync(runId, request, It.IsAny<CancellationToken>())).ReturnsAsync(package);

        AgentResult result = new()
        {
            RunId = runId,
            TaskId = task.TaskId,
            AgentType = AgentType.Topology,
            Confidence = 0.7,
            ResultId = "r1",
            CreatedUtc = DateTime.UtcNow,
        };

        Mock<IAgentExecutor> executor = new();
        executor.Setup(x => x.ExecuteAsync(
                runId,
                request,
                package,
                It.IsAny<IReadOnlyCollection<AgentTask>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentResult> { result });

        Mock<IAgentEvaluationService> evaluationService = new();
        evaluationService.Setup(x => x.EvaluateAsync(
                runId,
                request,
                package,
                It.IsAny<IReadOnlyCollection<AgentTask>>(),
                It.IsAny<IReadOnlyCollection<AgentResult>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IAgentEvidencePackageRepository> evidencePackageRepo = new();
        Mock<IAgentResultRepository> resultRepo = new();
        Mock<IAgentEvaluationRepository> evaluationRepo = new();

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("actor");

        ArchitectureRunService sut = CreateSut(
            executor.Object,
            runRepo.Object,
            requestRepo.Object,
            taskRepo.Object,
            evidenceBuilder.Object,
            evaluationService.Object,
            evidencePackageRepo.Object,
            resultRepo.Object,
            evaluationRepo.Object,
            actor.Object);

        ExecuteRunResult exec = await sut.ExecuteRunAsync(runId, CancellationToken.None);

        exec.Results.Should().ContainSingle();
        executor.Verify(
            x => x.ExecuteAsync(
                runId,
                request,
                package,
                It.IsAny<IReadOnlyCollection<AgentTask>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        evidencePackageRepo.Verify(x => x.CreateAsync(It.IsAny<AgentEvidencePackage>(), It.IsAny<CancellationToken>()), Times.Once);
        resultRepo.Verify(x => x.CreateManyAsync(It.IsAny<IReadOnlyList<AgentResult>>(), It.IsAny<CancellationToken>()), Times.Once);
        runRepo.Verify(
            x => x.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.ReadyForCommit,
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>(),
                ArchitectureRunStatus.TasksGenerated),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteRunAsync_when_ready_for_commit_replays_without_executor()
    {
        string runId = Guid.NewGuid().ToString("N");
        Mock<IArchitectureRunRepository> runRepo = new();
        runRepo.Setup(x => x.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRun
                {
                    RunId = runId,
                    RequestId = "r",
                    Status = ArchitectureRunStatus.ReadyForCommit,
                    CreatedUtc = DateTime.UtcNow,
                });

        AgentResult existing = new() { RunId = runId, ResultId = "x", AgentType = AgentType.Topology, CreatedUtc = DateTime.UtcNow };
        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentResult> { existing });

        Mock<IAgentExecutor> executor = new();
        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSut(
            executor.Object,
            runRepo.Object,
            Mock.Of<IArchitectureRequestRepository>(),
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IEvidenceBuilder>(),
            Mock.Of<IAgentEvaluationService>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            resultRepo.Object,
            Mock.Of<IAgentEvaluationRepository>(),
            actor.Object);

        ExecuteRunResult exec = await sut.ExecuteRunAsync(runId, CancellationToken.None);

        exec.Results.Should().ContainSingle();
        executor.Verify(
            x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<ArchitectureRequest>(),
                It.IsAny<AgentEvidencePackage>(),
                It.IsAny<IReadOnlyCollection<AgentTask>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteRunAsync_when_run_missing_throws_RunNotFoundException()
    {
        Mock<IArchitectureRunRepository> runRepo = new();
        runRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ArchitectureRun?)null);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSut(
            Mock.Of<IAgentExecutor>(),
            runRepo.Object,
            Mock.Of<IArchitectureRequestRepository>(),
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IEvidenceBuilder>(),
            Mock.Of<IAgentEvaluationService>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            Mock.Of<IAgentResultRepository>(),
            Mock.Of<IAgentEvaluationRepository>(),
            actor.Object);

        Func<Task> act = async () => await sut.ExecuteRunAsync("missing", CancellationToken.None);

        await act.Should().ThrowAsync<RunNotFoundException>();
    }

    [Fact]
    public async Task ExecuteRunAsync_when_terminal_without_results_throws_ConflictException()
    {
        string runId = Guid.NewGuid().ToString("N");
        Mock<IArchitectureRunRepository> runRepo = new();
        runRepo.Setup(x => x.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRun
                {
                    RunId = runId,
                    RequestId = "r",
                    Status = ArchitectureRunStatus.ReadyForCommit,
                    CreatedUtc = DateTime.UtcNow,
                });

        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSut(
            Mock.Of<IAgentExecutor>(),
            runRepo.Object,
            Mock.Of<IArchitectureRequestRepository>(),
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IEvidenceBuilder>(),
            Mock.Of<IAgentEvaluationService>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            resultRepo.Object,
            Mock.Of<IAgentEvaluationRepository>(),
            actor.Object);

        Func<Task> act = async () => await sut.ExecuteRunAsync(runId, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CommitRunAsync_happy_path_persists_manifest_and_marks_committed()
    {
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-cm-" + Guid.NewGuid().ToString("N");
        string manifestVersion = "v1-" + runId;

        Mock<IArchitectureRunRepository> runRepo = new();
        runRepo.Setup(x => x.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRun
                {
                    RunId = runId,
                    RequestId = requestId,
                    Status = ArchitectureRunStatus.ReadyForCommit,
                    CreatedUtc = DateTime.UtcNow,
                });

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

        AgentTask task = new()
        {
            TaskId = "t-cm",
            RunId = runId,
            AgentType = AgentType.Topology,
            Objective = "o",
            Status = AgentTaskStatus.Completed,
            CreatedUtc = DateTime.UtcNow,
        };

        Mock<IAgentTaskRepository> taskRepo = new();
        taskRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentTask> { task });

        AgentResult topologyResult = new()
        {
            RunId = runId,
            TaskId = task.TaskId,
            AgentType = AgentType.Topology,
            Confidence = 0.8,
            ResultId = "r-cm",
            CreatedUtc = DateTime.UtcNow,
        };

        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentResult> { topologyResult });

        Mock<IAgentEvaluationRepository> evaluationRepo = new();
        evaluationRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Mock<IAgentEvidencePackageRepository> evidencePackageRepo = new();
        evidencePackageRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentEvidencePackage { RunId = runId, RequestId = requestId });

        GoldenManifest manifest = new()
        {
            RunId = runId,
            SystemName = "S",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata { ManifestVersion = manifestVersion },
        };

        DecisionMergeResult merge = new()
        {
            Manifest = manifest,
            DecisionTraces =
            [
                new RunEventTrace
                {
                    RunId = runId,
                    EventType = "Commit",
                    EventDescription = "merged",
                },
            ],
        };

        Mock<IDecisionEngineService> decisionEngine = new();
        decisionEngine.Setup(x => x.MergeResults(
                runId,
                request,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<AgentResult>>(),
                It.IsAny<IReadOnlyCollection<AgentEvaluation>>(),
                It.IsAny<IReadOnlyCollection<DecisionNode>>(),
                It.IsAny<string?>()))
            .Returns(merge);

        Mock<IDecisionNodeRepository> decisionNodeRepo = new();
        Mock<ICoordinatorGoldenManifestRepository> manifestRepo = new();
        Mock<ICoordinatorDecisionTraceRepository> traceRepo = new();

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSutForCommit(
            runRepo.Object,
            requestRepo.Object,
            taskRepo.Object,
            resultRepo.Object,
            evaluationRepo.Object,
            evidencePackageRepo.Object,
            decisionEngine.Object,
            decisionNodeRepo.Object,
            manifestRepo.Object,
            traceRepo.Object,
            actor.Object);

        CommitRunResult committed = await sut.CommitRunAsync(runId, CancellationToken.None);

        committed.Manifest.Metadata.ManifestVersion.Should().Be(manifestVersion);
        decisionNodeRepo.Verify(x => x.CreateManyAsync(It.IsAny<IReadOnlyCollection<DecisionNode>>(), It.IsAny<CancellationToken>()), Times.Once);
        manifestRepo.Verify(
            x => x.CreateAsync(It.Is<GoldenManifest>(m => m.Metadata.ManifestVersion == manifestVersion), It.IsAny<CancellationToken>()),
            Times.Once);
        traceRepo.Verify(x => x.CreateManyAsync(It.IsAny<IEnumerable<RunEventTrace>>(), It.IsAny<CancellationToken>()), Times.Once);
        runRepo.Verify(
            x => x.UpdateStatusAsync(
                runId,
                ArchitectureRunStatus.Committed,
                manifestVersion,
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>(),
                ArchitectureRunStatus.ReadyForCommit),
            Times.Once);
    }

    [Fact]
    public async Task CommitRunAsync_when_status_not_ready_throws_ConflictException()
    {
        string runId = Guid.NewGuid().ToString("N");
        Mock<IArchitectureRunRepository> runRepo = new();
        runRepo.Setup(x => x.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRun
                {
                    RunId = runId,
                    RequestId = "r",
                    Status = ArchitectureRunStatus.TasksGenerated,
                    CreatedUtc = DateTime.UtcNow,
                });

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSutForCommit(
            runRepo.Object,
            Mock.Of<IArchitectureRequestRepository>(),
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IAgentResultRepository>(),
            Mock.Of<IAgentEvaluationRepository>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            Mock.Of<IDecisionEngineService>(),
            Mock.Of<IDecisionNodeRepository>(),
            Mock.Of<ICoordinatorGoldenManifestRepository>(),
            Mock.Of<ICoordinatorDecisionTraceRepository>(),
            actor.Object);

        Func<Task> act = async () => await sut.CommitRunAsync(runId, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CommitRunAsync_when_run_missing_throws_RunNotFoundException()
    {
        Mock<IArchitectureRunRepository> runRepo = new();
        runRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ArchitectureRun?)null);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSutForCommit(
            runRepo.Object,
            Mock.Of<IArchitectureRequestRepository>(),
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IAgentResultRepository>(),
            Mock.Of<IAgentEvaluationRepository>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            Mock.Of<IDecisionEngineService>(),
            Mock.Of<IDecisionNodeRepository>(),
            Mock.Of<ICoordinatorGoldenManifestRepository>(),
            Mock.Of<ICoordinatorDecisionTraceRepository>(),
            actor.Object);

        Func<Task> act = async () => await sut.CommitRunAsync("nope", CancellationToken.None);

        await act.Should().ThrowAsync<RunNotFoundException>();
    }

    private static ArchitectureRunService CreateSut(
        IAgentExecutor executor,
        IArchitectureRunRepository runRepository,
        IArchitectureRequestRepository requestRepository,
        IAgentTaskRepository taskRepository,
        IEvidenceBuilder evidenceBuilder,
        IAgentEvaluationService agentEvaluationService,
        IAgentEvidencePackageRepository agentEvidencePackageRepository,
        IAgentResultRepository resultRepository,
        IAgentEvaluationRepository agentEvaluationRepository,
        IActorContext actorContext)
    {
        return new ArchitectureRunService(
            Mock.Of<ICoordinatorService>(),
            executor,
            Mock.Of<IDecisionEngineService>(),
            agentEvaluationService,
            agentEvaluationRepository,
            new DecisionEngineV2(),
            Mock.Of<IDecisionNodeRepository>(),
            evidenceBuilder,
            requestRepository,
            runRepository,
            taskRepository,
            resultRepository,
            Mock.Of<ICoordinatorGoldenManifestRepository>(),
            Mock.Of<IEvidenceBundleRepository>(),
            Mock.Of<ICoordinatorDecisionTraceRepository>(),
            agentEvidencePackageRepository,
            Mock.Of<IArchitectureRunIdempotencyRepository>(),
            actorContext,
            Mock.Of<IBaselineMutationAuditService>(),
            NullLogger<ArchitectureRunService>.Instance);
    }

    private static ArchitectureRunService CreateSutForCommit(
        IArchitectureRunRepository runRepository,
        IArchitectureRequestRepository requestRepository,
        IAgentTaskRepository taskRepository,
        IAgentResultRepository resultRepository,
        IAgentEvaluationRepository agentEvaluationRepository,
        IAgentEvidencePackageRepository agentEvidencePackageRepository,
        IDecisionEngineService decisionEngine,
        IDecisionNodeRepository decisionNodeRepository,
        ICoordinatorGoldenManifestRepository manifestRepository,
        ICoordinatorDecisionTraceRepository decisionTraceRepository,
        IActorContext actorContext)
    {
        return new ArchitectureRunService(
            Mock.Of<ICoordinatorService>(),
            Mock.Of<IAgentExecutor>(),
            decisionEngine,
            Mock.Of<IAgentEvaluationService>(),
            agentEvaluationRepository,
            new DecisionEngineV2(),
            decisionNodeRepository,
            Mock.Of<IEvidenceBuilder>(),
            requestRepository,
            runRepository,
            taskRepository,
            resultRepository,
            manifestRepository,
            Mock.Of<IEvidenceBundleRepository>(),
            decisionTraceRepository,
            agentEvidencePackageRepository,
            Mock.Of<IArchitectureRunIdempotencyRepository>(),
            actorContext,
            Mock.Of<IBaselineMutationAuditService>(),
            NullLogger<ArchitectureRunService>.Instance);
    }
}
