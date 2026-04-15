using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application;
using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Coordinator.Services;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.TestSupport;

using FluentAssertions;

using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Runs;

/// <summary>
/// FsCheck-backed invariants for authority run commit gates (status + agent results), complementing
/// <see cref="ArchitectureRunServiceExecuteCommitTests"/>.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class RunLifecycleStatePropertyTests
{
#pragma warning disable xUnit1031

    private static readonly ScopeContext AuthorityTestScope = new()
    {
        TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    };

    [Property(MaxTest = 30)]
    public void CommitRunAsync_throws_Conflict_when_ReadyForCommit_and_agent_results_empty(Guid runGuid)
    {
        string runId = runGuid.ToString("N");
        string requestId = "req-lc-" + runId[..Math.Min(8, runId.Length)];

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

        Mock<IAgentTaskRepository> taskRepo = new();
        taskRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("lifecycle-prop-tester");

        ArchitectureRunService sut = CreateSutForCommit(
            runRepo,
            scopeProvider,
            requestRepo.Object,
            taskRepo.Object,
            resultRepo.Object,
            Mock.Of<IAgentEvaluationRepository>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            Mock.Of<IDecisionEngineService>(),
            Mock.Of<IDecisionNodeRepository>(),
            Mock.Of<ICoordinatorGoldenManifestRepository>(),
            Mock.Of<ICoordinatorDecisionTraceRepository>(),
            actor.Object);

        Action act = () => sut.CommitRunAsync(runId, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<ConflictException>()
            .WithMessage($"*No agent results found for run '{runId}'*");
    }

    [Property(MaxTest = 25)]
    public void CommitRunAsync_throws_Conflict_when_status_is_Failed(Guid runGuid)
    {
        string runId = runGuid.ToString("N");

        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = "r",
            Status = ArchitectureRunStatus.Failed,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("lifecycle-prop-tester");

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

        Action act = () => sut.CommitRunAsync(runId, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<ConflictException>()
            .WithMessage("*Failed status*");
    }

    [Property(MaxTest = 25)]
    public void CommitRunAsync_throws_Conflict_when_status_is_Created(Guid runGuid)
    {
        string runId = runGuid.ToString("N");

        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = "r",
            Status = ArchitectureRunStatus.Created,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("lifecycle-prop-tester");

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

        Action act = () => sut.CommitRunAsync(runId, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<ConflictException>()
            .WithMessage("*cannot be committed in status*Created*");
    }

    [Property(MaxTest = 25)]
    public void CommitRunAsync_throws_Conflict_when_status_is_Executing(Guid runGuid)
    {
        string runId = runGuid.ToString("N");

        ArchitectureRun runModel = new()
        {
            RunId = runId,
            RequestId = "r",
            Status = ArchitectureRunStatus.Executing,
            CreatedUtc = DateTime.UtcNow,
        };

        (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

        Mock<IActorContext> actor = new();
        actor.Setup(x => x.GetActor()).Returns("lifecycle-prop-tester");

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

        Action act = () => sut.CommitRunAsync(runId, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<ConflictException>()
            .WithMessage("*cannot be committed in status*Executing*");
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
                Mock.Of<IPreCommitGovernanceGate>(),
                Options.Create(new PreCommitGovernanceGateOptions()),
                Mock.Of<IAuditService>(),
                NullLogger<ArchitectureRunCommitOrchestrator>.Instance));
    }
}
