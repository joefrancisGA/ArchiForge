using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Coordinator.Services;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Concurrency;
using ArchLucid.Core.Metering;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.TestSupport;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ArchLucid.Application.Tests;

/// <summary>
/// <see cref="ArchitectureRunService.ExecuteRunAsync"/> and <see cref="ArchitectureRunService.CommitRunAsync"/> behavior with mocked persistence and engines.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ArchitectureRunServiceExecuteCommitTests
{
    private static readonly ScopeContext AuthorityTestScope = new()
    {
        TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
    };

    [Fact]
    public async Task ExecuteRunAsync_happy_path_persists_evidence_results_and_evaluations()
    {
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-ex-" + Guid.NewGuid().ToString("N");
        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = requestId,
            Status = ArchitectureRunStatus.TasksGenerated,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

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
            runRepo,
            scopeProvider,
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
    }

    [Fact]
    public async Task ExecuteRunAsync_after_successful_persist_invokes_output_trace_evaluation_hook()
    {
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-hook-" + Guid.NewGuid().ToString("N");
        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = requestId,
            Status = ArchitectureRunStatus.TasksGenerated,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

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
            TaskId = "t-hook",
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
            ResultId = "r-hook",
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

        Mock<IAgentOutputTraceEvaluationHook> hook = new();
        hook.Setup(h => h.AfterSuccessfulExecuteAsync(runId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        ArchitectureRunService sut = CreateSut(
            executor.Object,
            runRepo,
            scopeProvider,
            requestRepo.Object,
            taskRepo.Object,
            evidenceBuilder.Object,
            evaluationService.Object,
            evidencePackageRepo.Object,
            resultRepo.Object,
            evaluationRepo.Object,
            actor.Object,
            hook.Object);

        _ = await sut.ExecuteRunAsync(runId, CancellationToken.None);

        hook.Verify(h => h.AfterSuccessfulExecuteAsync(runId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteRunAsync_after_persist_sets_LegacyRunStatus_ReadyForCommit_when_four_required_agents_complete()
    {
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-four-" + Guid.NewGuid().ToString("N");
        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = requestId,
            Status = ArchitectureRunStatus.TasksGenerated,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

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

        static AgentTask TaskFor(string id, AgentType type, string rid) => new()
        {
            TaskId = id,
            RunId = rid,
            AgentType = type,
            Objective = "o",
            Status = AgentTaskStatus.Created,
            CreatedUtc = DateTime.UtcNow,
            EvidenceBundleRef = "eb",
        };

        List<AgentTask> tasks =
        [
            TaskFor("t-topo", AgentType.Topology, runId),
            TaskFor("t-cost", AgentType.Cost, runId),
            TaskFor("t-comp", AgentType.Compliance, runId),
            TaskFor("t-crit", AgentType.Critic, runId),
        ];

        Mock<IAgentTaskRepository> taskRepo = new();
        taskRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

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

        static AgentResult Res(string taskId, AgentType type, string rid) => new()
        {
            RunId = rid,
            TaskId = taskId,
            AgentType = type,
            Confidence = 0.7,
            ResultId = "r-" + taskId,
            CreatedUtc = DateTime.UtcNow,
        };

        List<AgentResult> fourResults =
        [
            Res("t-topo", AgentType.Topology, runId),
            Res("t-cost", AgentType.Cost, runId),
            Res("t-comp", AgentType.Compliance, runId),
            Res("t-crit", AgentType.Critic, runId),
        ];

        Mock<IAgentExecutor> executor = new();
        executor.Setup(x => x.ExecuteAsync(
                runId,
                request,
                package,
                It.IsAny<IReadOnlyCollection<AgentTask>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fourResults);

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
            runRepo,
            scopeProvider,
            requestRepo.Object,
            taskRepo.Object,
            evidenceBuilder.Object,
            evaluationService.Object,
            evidencePackageRepo.Object,
            resultRepo.Object,
            evaluationRepo.Object,
            actor.Object);

        ExecuteRunResult exec = await sut.ExecuteRunAsync(runId, CancellationToken.None);

        exec.Results.Should().HaveCount(4);

        Mock.Get(runRepo).Verify(
            r => r.UpdateAsync(
                It.Is<RunRecord>(h =>
                    h.RunId == Guid.ParseExact(runId, "N")
                    && string.Equals(h.LegacyRunStatus, ArchitectureRunStatus.ReadyForCommit.ToString(), StringComparison.Ordinal)),
                It.IsAny<CancellationToken>(),
                null,
                null),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteRunAsync_when_ready_for_commit_replays_without_executor()
    {
        string runId = Guid.NewGuid().ToString("N");
        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = "r",
            Status = ArchitectureRunStatus.ReadyForCommit,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

        AgentResult existing = new()
        {
            RunId = runId,
            ResultId = "x",
            AgentType = AgentType.Topology,
            CreatedUtc = DateTime.UtcNow
        };
        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentResult> { existing });

        Mock<IAgentExecutor> executor = new();
        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSut(
            executor.Object,
            runRepo,
            scopeProvider,
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
        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(null);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSut(
            Mock.Of<IAgentExecutor>(),
            runRepo,
            scopeProvider,
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
        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = "r",
            Status = ArchitectureRunStatus.ReadyForCommit,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSut(
            Mock.Of<IAgentExecutor>(),
            runRepo,
            scopeProvider,
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
        string decisionTraceId = "trace-commit-happy-" + runId;

        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = requestId,
            Status = ArchitectureRunStatus.ReadyForCommit,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

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
            Metadata = new ManifestMetadata
            {
                ManifestVersion = manifestVersion,
                DecisionTraceIds = [decisionTraceId],
            },
        };

        DecisionMergeResult merge = new()
        {
            Manifest = manifest,
            DecisionTraces =
            [
                RunEventTrace.From(new RunEventTracePayload
                {
                    TraceId = decisionTraceId,
                    RunId = runId,
                    EventType = "Commit",
                    EventDescription = "merged",
                }),
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
            runRepo,
            scopeProvider,
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
        traceRepo.Verify(x => x.CreateManyAsync(It.IsAny<IEnumerable<DecisionTrace>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitRunAsync_when_status_not_ready_throws_ConflictException()
    {
        string runId = Guid.NewGuid().ToString("N");
        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = "r",
            Status = ArchitectureRunStatus.Created,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSutForCommit(
            runRepo,
            scopeProvider,
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
        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(null);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSutForCommit(
            runRepo,
            scopeProvider,
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

    [Fact]
    public async Task CommitRunAsync_throws_PreCommitGovernanceBlockedException_when_gate_blocks()
    {
        string runId = Guid.NewGuid().ToString("N");
        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = "r",
            Status = ArchitectureRunStatus.ReadyForCommit,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

        Mock<IPreCommitGovernanceGate> gate = new();
        gate.Setup(g => g.EvaluateAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PreCommitGateResult
                {
                    Blocked = true,
                    Reason =
                        "1 Critical finding(s) block commit per policy pack assignment (pack test-pack).",
                    BlockingFindingIds = ["f-critical-1"],
                    PolicyPackId = "test-pack",
                });

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSutForCommit(
            runRepo,
            scopeProvider,
            Mock.Of<IArchitectureRequestRepository>(),
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IAgentResultRepository>(),
            Mock.Of<IAgentEvaluationRepository>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            Mock.Of<IDecisionEngineService>(),
            Mock.Of<IDecisionNodeRepository>(),
            Mock.Of<ICoordinatorGoldenManifestRepository>(),
            Mock.Of<ICoordinatorDecisionTraceRepository>(),
            actor.Object,
            gate.Object,
            Options.Create(new PreCommitGovernanceGateOptions { PreCommitGateEnabled = true }),
            Mock.Of<IAuditService>());

        Func<Task> act = async () => await sut.CommitRunAsync(runId, CancellationToken.None);

        PreCommitGovernanceBlockedException ex = (await act.Should().ThrowAsync<PreCommitGovernanceBlockedException>())
            .Which;

        ex.Result.Blocked.Should().BeTrue();
        ex.Result.BlockingFindingIds.Should().ContainSingle().Which.Should().Be("f-critical-1");
        ex.Result.PolicyPackId.Should().Be("test-pack");
    }

    [Fact]
    public async Task CommitRunAsync_succeeds_when_gate_allows()
    {
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-gate-allow-" + Guid.NewGuid().ToString("N");
        string manifestVersion = "v1-" + runId;
        string decisionTraceId = "trace-gate-allow-" + runId;

        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = requestId,
            Status = ArchitectureRunStatus.ReadyForCommit,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

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
            TaskId = "t-gate",
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
            ResultId = "r-gate",
            CreatedUtc = DateTime.UtcNow,
        };

        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentResult> { topologyResult });

        Mock<IAgentEvaluationRepository> evaluationRepo = new();
        evaluationRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Mock<IAgentEvidencePackageRepository> evidencePackageRepo = new();
        evidencePackageRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentEvidencePackage { RunId = runId, RequestId = requestId });

        GoldenManifest manifest = new()
        {
            RunId = runId,
            SystemName = "S",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new ManifestGovernance(),
            Metadata = new ManifestMetadata
            {
                ManifestVersion = manifestVersion,
                DecisionTraceIds = [decisionTraceId],
            },
        };

        DecisionMergeResult merge = new()
        {
            Manifest = manifest,
            DecisionTraces =
            [
                RunEventTrace.From(new RunEventTracePayload
                {
                    TraceId = decisionTraceId,
                    RunId = runId,
                    EventType = "Commit",
                    EventDescription = "merged",
                }),
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

        Mock<IPreCommitGovernanceGate> gate = new();
        gate.Setup(g => g.EvaluateAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(PreCommitGateResult.Allowed());

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSutForCommit(
            runRepo,
            scopeProvider,
            requestRepo.Object,
            taskRepo.Object,
            resultRepo.Object,
            evaluationRepo.Object,
            evidencePackageRepo.Object,
            decisionEngine.Object,
            decisionNodeRepo.Object,
            manifestRepo.Object,
            traceRepo.Object,
            actor.Object,
            gate.Object,
            Options.Create(new PreCommitGovernanceGateOptions { PreCommitGateEnabled = true }),
            Mock.Of<IAuditService>());

        CommitRunResult committed = await sut.CommitRunAsync(runId, CancellationToken.None);

        committed.Manifest.Metadata.ManifestVersion.Should().Be(manifestVersion);
    }

    [Fact]
    public async Task CommitRunAsync_skips_gate_when_disabled()
    {
        string runId = Guid.NewGuid().ToString("N");
        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = "r",
            Status = ArchitectureRunStatus.ReadyForCommit,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

        Mock<IPreCommitGovernanceGate> gate = new();

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("a");

        ArchitectureRunService sut = CreateSutForCommit(
            runRepo,
            scopeProvider,
            Mock.Of<IArchitectureRequestRepository>(),
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IAgentResultRepository>(),
            Mock.Of<IAgentEvaluationRepository>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            Mock.Of<IDecisionEngineService>(),
            Mock.Of<IDecisionNodeRepository>(),
            Mock.Of<ICoordinatorGoldenManifestRepository>(),
            Mock.Of<ICoordinatorDecisionTraceRepository>(),
            actor.Object,
            gate.Object,
            Options.Create(new PreCommitGovernanceGateOptions { PreCommitGateEnabled = false }),
            Mock.Of<IAuditService>());

        Func<Task> actCommit = async () => await sut.CommitRunAsync(runId, CancellationToken.None);

        await actCommit.Should().ThrowAsync<InvalidOperationException>();

        gate.Verify(g => g.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (IRunRepository Repo, IScopeContextProvider Scope) CreateRunAuthorityMocks(ArchitectureRun? run)
    {
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(AuthorityTestScope);
        Mock<IRunRepository> runRepo = new();

        if (run is null)
        {
            runRepo.Setup(r => r.GetByIdAsync(AuthorityTestScope, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RunRecord?)null);

            return (runRepo.Object, scopeProvider.Object);
        }

        Guid g = Guid.ParseExact(run.RunId, "N");
        runRepo.Setup(r => r.GetByIdAsync(AuthorityTestScope, g, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToRunRecord(run));

        return (runRepo.Object, scopeProvider.Object);
    }

    private static RunRecord ToRunRecord(ArchitectureRun run)
    {
        return new RunRecord
        {
            RunId = Guid.ParseExact(run.RunId, "N"),
            TenantId = AuthorityTestScope.TenantId,
            WorkspaceId = AuthorityTestScope.WorkspaceId,
            ScopeProjectId = AuthorityTestScope.ProjectId,
            ProjectId = "test-project",
            ArchitectureRequestId = run.RequestId,
            LegacyRunStatus = run.Status.ToString(),
            CreatedUtc = run.CreatedUtc,
            CompletedUtc = run.CompletedUtc,
            CurrentManifestVersion = run.CurrentManifestVersion,
            ContextSnapshotId = ParseOptionalContextSnapshot(run.ContextSnapshotId),
            GraphSnapshotId = run.GraphSnapshotId,
            FindingsSnapshotId = run.FindingsSnapshotId,
            GoldenManifestId = run.GoldenManifestId,
            DecisionTraceId = run.DecisionTraceId,
            ArtifactBundleId = run.ArtifactBundleId,
        };
    }

    private static Guid? ParseOptionalContextSnapshot(string? contextSnapshotId) =>
        string.IsNullOrWhiteSpace(contextSnapshotId) ? null : Guid.ParseExact(contextSnapshotId, "N");

    private static ArchitectureRunService CreateSut(
        IAgentExecutor executor,
        IRunRepository runRepository,
        IScopeContextProvider scopeContextProvider,
        IArchitectureRequestRepository requestRepository,
        IAgentTaskRepository taskRepository,
        IEvidenceBuilder evidenceBuilder,
        IAgentEvaluationService agentEvaluationService,
        IAgentEvidencePackageRepository agentEvidencePackageRepository,
        IAgentResultRepository resultRepository,
        IAgentEvaluationRepository agentEvaluationRepository,
        IActorContext actorContext,
        IAgentOutputTraceEvaluationHook? outputTraceEvaluationHook = null)
    {
        IBaselineMutationAuditService audit = Mock.Of<IBaselineMutationAuditService>();

        return new ArchitectureRunService(
            new ArchitectureRunCreateOrchestrator(
                Mock.Of<ICoordinatorService>(),
                requestRepository,
                runRepository,
                scopeContextProvider,
                Mock.Of<IEvidenceBundleRepository>(),
                taskRepository,
                Mock.Of<IArchitectureRunIdempotencyRepository>(),
                actorContext,
                audit,
                Mock.Of<IAuditService>(),
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                Mock.Of<IUsageMeteringService>(),
                new NoOpDistributedCreateRunIdempotencyLock(),
                TimeProvider.System,
                NullLogger<ArchitectureRunCreateOrchestrator>.Instance),
            new ArchitectureRunExecuteOrchestrator(
                runRepository,
                scopeContextProvider,
                requestRepository,
                taskRepository,
                executor,
                agentEvaluationService,
                resultRepository,
                agentEvaluationRepository,
                agentEvidencePackageRepository,
                evidenceBuilder,
                actorContext,
                audit,
                Mock.Of<IAuditService>(),
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                outputTraceEvaluationHook ?? new NoOpAgentOutputTraceEvaluationHook(),
                NullLogger<ArchitectureRunExecuteOrchestrator>.Instance),
            new ArchitectureRunCommitOrchestrator(
                runRepository,
                scopeContextProvider,
                requestRepository,
                taskRepository,
                resultRepository,
                agentEvaluationRepository,
                agentEvidencePackageRepository,
                Mock.Of<IDecisionEngineService>(),
                new DecisionEngineV2(),
                Mock.Of<IDecisionNodeRepository>(),
                Mock.Of<ICoordinatorGoldenManifestRepository>(),
                Mock.Of<ICoordinatorDecisionTraceRepository>(),
                actorContext,
                audit,
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                Mock.Of<IPreCommitGovernanceGate>(),
                Options.Create(new PreCommitGovernanceGateOptions()),
                Mock.Of<IAuditService>(),
                NullLogger<ArchitectureRunCommitOrchestrator>.Instance));
    }

    private static ArchitectureRunService CreateSutForCommit(
        IRunRepository runRepository,
        IScopeContextProvider scopeContextProvider,
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
        return CreateSutForCommit(
            runRepository,
            scopeContextProvider,
            requestRepository,
            taskRepository,
            resultRepository,
            agentEvaluationRepository,
            agentEvidencePackageRepository,
            decisionEngine,
            decisionNodeRepository,
            manifestRepository,
            decisionTraceRepository,
            actorContext,
            Mock.Of<IPreCommitGovernanceGate>(),
            Options.Create(new PreCommitGovernanceGateOptions()),
            Mock.Of<IAuditService>());
    }

    private static ArchitectureRunService CreateSutForCommit(
        IRunRepository runRepository,
        IScopeContextProvider scopeContextProvider,
        IArchitectureRequestRepository requestRepository,
        IAgentTaskRepository taskRepository,
        IAgentResultRepository resultRepository,
        IAgentEvaluationRepository agentEvaluationRepository,
        IAgentEvidencePackageRepository agentEvidencePackageRepository,
        IDecisionEngineService decisionEngine,
        IDecisionNodeRepository decisionNodeRepository,
        ICoordinatorGoldenManifestRepository manifestRepository,
        ICoordinatorDecisionTraceRepository decisionTraceRepository,
        IActorContext actorContext,
        IPreCommitGovernanceGate preCommitGovernanceGate,
        IOptions<PreCommitGovernanceGateOptions> preCommitGovernanceGateOptions,
        IAuditService auditService)
    {
        IBaselineMutationAuditService audit = Mock.Of<IBaselineMutationAuditService>();

        return new ArchitectureRunService(
            new ArchitectureRunCreateOrchestrator(
                Mock.Of<ICoordinatorService>(),
                Mock.Of<IArchitectureRequestRepository>(),
                runRepository,
                scopeContextProvider,
                Mock.Of<IEvidenceBundleRepository>(),
                taskRepository,
                Mock.Of<IArchitectureRunIdempotencyRepository>(),
                actorContext,
                audit,
                Mock.Of<IAuditService>(),
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                Mock.Of<IUsageMeteringService>(),
                new NoOpDistributedCreateRunIdempotencyLock(),
                TimeProvider.System,
                NullLogger<ArchitectureRunCreateOrchestrator>.Instance),
            new ArchitectureRunExecuteOrchestrator(
                runRepository,
                scopeContextProvider,
                requestRepository,
                taskRepository,
                Mock.Of<IAgentExecutor>(),
                Mock.Of<IAgentEvaluationService>(),
                resultRepository,
                agentEvaluationRepository,
                agentEvidencePackageRepository,
                Mock.Of<IEvidenceBuilder>(),
                actorContext,
                audit,
                Mock.Of<IAuditService>(),
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                new NoOpAgentOutputTraceEvaluationHook(),
                NullLogger<ArchitectureRunExecuteOrchestrator>.Instance),
            new ArchitectureRunCommitOrchestrator(
                runRepository,
                scopeContextProvider,
                requestRepository,
                taskRepository,
                resultRepository,
                agentEvaluationRepository,
                agentEvidencePackageRepository,
                decisionEngine,
                new DecisionEngineV2(),
                decisionNodeRepository,
                manifestRepository,
                decisionTraceRepository,
                actorContext,
                audit,
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                preCommitGovernanceGate,
                preCommitGovernanceGateOptions,
                auditService,
                NullLogger<ArchitectureRunCommitOrchestrator>.Instance));
    }
}
