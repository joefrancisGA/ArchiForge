using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Coordinator.Services;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Concurrency;
using ArchLucid.Core.Metering;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Orchestration;

/// <summary>
/// Verifies that the coordinator orchestrators emit durable <see cref="IAuditService.LogAsync"/>
/// calls with the correct <c>CoordinatorRun*</c> event types alongside the existing baseline mutation audit.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class CoordinatorAuditDurableTests
{
    private static readonly ScopeContext TestScope = new()
    {
        TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    };

    [Fact]
    public async Task CreateOrchestrator_emits_CoordinatorRunCreated_on_success()
    {
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-" + Guid.NewGuid().ToString("N");

        ArchitectureRequest request = new()
        {
            RequestId = requestId,
            SystemName = "TestSystem",
            Environment = "dev",
            CloudProvider = CloudProvider.Azure,
            Description = "d",
        };

        CoordinationResult coordination = new()
        {
            Run = new ArchitectureRun
            {
                RunId = runId,
                RequestId = requestId,
                Status = ArchitectureRunStatus.TasksGenerated,
                CreatedUtc = DateTime.UtcNow,
            },
            EvidenceBundle = new EvidenceBundle { EvidenceBundleId = "eb-1" },
            Tasks = [],
        };

        Mock<ICoordinatorService> coordinator = new();
        coordinator.Setup(c => c.CreateRunAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(coordination);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("test-actor");

        Mock<IAuditService> auditService = new();

        ArchitectureRunCreateOrchestrator sut = new(
            coordinator.Object,
            Mock.Of<IArchitectureRequestRepository>(),
            Mock.Of<IRunRepository>(),
            scopeProvider.Object,
            Mock.Of<IEvidenceBundleRepository>(),
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IArchitectureRunIdempotencyRepository>(),
            actor.Object,
            Mock.Of<IBaselineMutationAuditService>(),
            auditService.Object,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            Mock.Of<IUsageMeteringService>(),
            new NoOpDistributedCreateRunIdempotencyLock(),
            TimeProvider.System,
            NullLogger<ArchitectureRunCreateOrchestrator>.Instance);

        CreateRunResult result = await sut.CreateRunAsync(request);

        result.Should().NotBeNull();

        auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.CoordinatorRunCreated &&
                    e.TenantId == TestScope.TenantId &&
                    e.WorkspaceId == TestScope.WorkspaceId &&
                    e.ProjectId == TestScope.ProjectId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteOrchestrator_emits_Started_and_Succeeded_on_success()
    {
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-exec-" + Guid.NewGuid().ToString("N");

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

        AgentEvidencePackage evidence = new()
        {
            RunId = runId,
            RequestId = requestId,
            SystemName = "S",
            Environment = "prod",
            CloudProvider = "Azure",
            Request = new RequestEvidence { Description = "d" },
        };

        Mock<IEvidenceBuilder> evidenceBuilder = new();
        evidenceBuilder.Setup(x => x.BuildAsync(runId, request, It.IsAny<CancellationToken>())).ReturnsAsync(evidence);

        AgentResult agentResult = new()
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
                evidence,
                It.IsAny<IReadOnlyCollection<AgentTask>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentResult> { agentResult });

        Mock<IAgentEvaluationService> evalService = new();
        evalService.Setup(x => x.EvaluateAsync(
                runId, request, evidence,
                It.IsAny<IReadOnlyCollection<AgentTask>>(),
                It.IsAny<IReadOnlyCollection<AgentResult>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("test-actor");

        Mock<IAuditService> auditService = new();

        ArchitectureRunExecuteOrchestrator sut = new(
            runRepo,
            scopeProvider,
            requestRepo.Object,
            taskRepo.Object,
            executor.Object,
            evalService.Object,
            Mock.Of<IAgentResultRepository>(),
            Mock.Of<IAgentEvaluationRepository>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            evidenceBuilder.Object,
            actor.Object,
            Mock.Of<IBaselineMutationAuditService>(),
            auditService.Object,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            new NoOpAgentOutputTraceEvaluationHook(),
            NullLogger<ArchitectureRunExecuteOrchestrator>.Instance);

        ExecuteRunResult result = await sut.ExecuteRunAsync(runId);

        result.Results.Should().ContainSingle();

        auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.CoordinatorRunExecuteStarted),
                It.IsAny<CancellationToken>()),
            Times.Once);

        auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.CoordinatorRunExecuteSucceeded),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitOrchestrator_emits_CoordinatorRunCommitCompleted_on_success()
    {
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-commit-" + Guid.NewGuid().ToString("N");
        string manifestVersion = $"v1-{runId}";
        Guid manifestId = Guid.NewGuid();

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
            TaskId = "t-c",
            RunId = runId,
            AgentType = AgentType.Topology,
            Objective = "o",
            Status = AgentTaskStatus.Created,
            CreatedUtc = DateTime.UtcNow,
            EvidenceBundleRef = "eb",
        };

        Mock<IAgentTaskRepository> taskRepo = new();
        taskRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentTask> { task });

        AgentResult agentResult = new()
        {
            RunId = runId,
            TaskId = task.TaskId,
            AgentType = AgentType.Topology,
            Confidence = 0.7,
            ResultId = "r1",
            CreatedUtc = DateTime.UtcNow,
        };

        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgentResult> { agentResult });

        AgentEvidencePackage evidence = new()
        {
            RunId = runId,
            RequestId = requestId,
            SystemName = "S",
            Environment = "prod",
            CloudProvider = "Azure",
            Request = new RequestEvidence { Description = "d" },
        };

        Mock<IAgentEvidencePackageRepository> evidencePackageRepo = new();
        evidencePackageRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(evidence);

        Mock<IAgentEvaluationRepository> evalRepo = new();
        evalRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        string decisionTraceId = Guid.NewGuid().ToString();

        GoldenManifest manifest = new()
        {
            RunId = runId,
            SystemName = "S",
            Metadata = new ManifestMetadata
            {
                ManifestVersion = manifestVersion,
                DecisionTraceIds = [decisionTraceId],
            },
        };
        DecisionTrace trace = RunEventTrace.From(new RunEventTracePayload
        {
            TraceId = decisionTraceId,
            RunId = runId,
            EventType = "CommitCompleted",
            EventDescription = "Test",
            CreatedUtc = DateTime.UtcNow,
        });

        DecisionMergeResult mergeResult = new()
        {
            Manifest = manifest,
            DecisionTraces = [trace],
            Warnings = [],
        };

        Mock<IDecisionEngineService> decisionEngine = new();
        decisionEngine.Setup(x => x.MergeResults(
                runId,
                request,
                manifestVersion,
                It.IsAny<IReadOnlyList<AgentResult>>(),
                It.IsAny<IReadOnlyList<AgentEvaluation>>(),
                It.IsAny<IReadOnlyList<DecisionNode>>(),
                It.IsAny<string?>()))
            .Returns(mergeResult);

        Mock<IDecisionEngineV2> decisionEngineV2 = new();
        decisionEngineV2.Setup(x => x.ResolveAsync(
                runId,
                request,
                It.IsAny<IReadOnlyList<AgentTask>>(),
                It.IsAny<IReadOnlyList<AgentResult>>(),
                It.IsAny<IReadOnlyList<AgentEvaluation>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("test-actor");

        Mock<IAuditService> auditService = new();

        ArchitectureRunCommitOrchestrator sut = new(
            runRepo,
            scopeProvider,
            requestRepo.Object,
            taskRepo.Object,
            resultRepo.Object,
            evalRepo.Object,
            evidencePackageRepo.Object,
            decisionEngine.Object,
            decisionEngineV2.Object,
            Mock.Of<IDecisionNodeRepository>(),
            Mock.Of<ICoordinatorGoldenManifestRepository>(),
            Mock.Of<ICoordinatorDecisionTraceRepository>(),
            actor.Object,
            Mock.Of<IBaselineMutationAuditService>(),
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            Mock.Of<IPreCommitGovernanceGate>(),
            Options.Create(new PreCommitGovernanceGateOptions()),
            auditService.Object,
            NoOpTrialFunnelCommitHook.Instance,
            NullLogger<ArchitectureRunCommitOrchestrator>.Instance);

        CommitRunResult result = await sut.CommitRunAsync(runId);

        result.Should().NotBeNull();

        auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e =>
                    e.EventType == AuditEventTypes.CoordinatorRunCommitCompleted &&
                    e.TenantId == TestScope.TenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrchestrator_audit_failure_does_not_break_main_flow()
    {
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-fail-" + Guid.NewGuid().ToString("N");

        ArchitectureRequest request = new()
        {
            RequestId = requestId,
            SystemName = "TestSystem",
            Environment = "dev",
            CloudProvider = CloudProvider.Azure,
            Description = "d",
        };

        CoordinationResult coordination = new()
        {
            Run = new ArchitectureRun
            {
                RunId = runId,
                RequestId = requestId,
                Status = ArchitectureRunStatus.TasksGenerated,
                CreatedUtc = DateTime.UtcNow,
            },
            EvidenceBundle = new EvidenceBundle { EvidenceBundleId = "eb-2" },
            Tasks = [],
        };

        Mock<ICoordinatorService> coordinator = new();
        coordinator.Setup(c => c.CreateRunAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(coordination);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("test-actor");

        Mock<IAuditService> auditService = new();
        auditService
            .Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated audit failure"));

        ArchitectureRunCreateOrchestrator sut = new(
            coordinator.Object,
            Mock.Of<IArchitectureRequestRepository>(),
            Mock.Of<IRunRepository>(),
            scopeProvider.Object,
            Mock.Of<IEvidenceBundleRepository>(),
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IArchitectureRunIdempotencyRepository>(),
            actor.Object,
            Mock.Of<IBaselineMutationAuditService>(),
            auditService.Object,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            Mock.Of<IUsageMeteringService>(),
            new NoOpDistributedCreateRunIdempotencyLock(),
            TimeProvider.System,
            NullLogger<ArchitectureRunCreateOrchestrator>.Instance);

        CreateRunResult result = await sut.CreateRunAsync(request);

        result.Should().NotBeNull();
        result.Run.RunId.Should().Be(runId);
    }

    /// <summary>
    /// Creates mocked <see cref="IRunRepository"/> and <see cref="IScopeContextProvider"/> that resolve
    /// a run by GUID lookup within the test scope.
    /// </summary>
    private static (IRunRepository, IScopeContextProvider) CreateRunAuthorityMocks(ArchitectureRun run)
    {
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);

        Guid runGuid = Guid.TryParse(run.RunId, out Guid parsed) ? parsed : Guid.Empty;

        RunRecord record = new()
        {
            RunId = runGuid,
            TenantId = TestScope.TenantId,
            WorkspaceId = TestScope.WorkspaceId,
            ScopeProjectId = TestScope.ProjectId,
            ProjectId = "default",
            ArchitectureRequestId = run.RequestId,
            LegacyRunStatus = run.Status.ToString(),
            CreatedUtc = run.CreatedUtc,
            CurrentManifestVersion = run.CurrentManifestVersion,
        };

        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(r => r.GetByIdAsync(TestScope, runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        runRepo.Setup(r => r.UpdateAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        return (runRepo.Object, scopeProvider.Object);
    }
}
