using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Metadata;
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

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Runs;

/// <summary>
/// FsCheck sweep: non-ready statuses either throw <see cref="ConflictException"/> on commit, or (for
/// <see cref="ArchitectureRunStatus.TasksGenerated"/>) fail later with <see cref="InvalidOperationException"/> when prerequisites are missing.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class RunLifecycleStateMachineCommandPropertyTests
{
    private static readonly ScopeContext AuthorityTestScope = new()
    {
        TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    };

    private static readonly Arbitrary<ArchitectureRunStatus> NonReadyCommitStatusesArb = Arb.From(
        Gen.Elements(
            ArchitectureRunStatus.Created,
            ArchitectureRunStatus.TasksGenerated,
            ArchitectureRunStatus.WaitingForResults,
            ArchitectureRunStatus.Failed,
            ArchitectureRunStatus.Committed));

    [Property(MaxTest = 25)]
    public Property CommitRunAsync_rejects_or_fails_prerequisites_for_blocked_statuses()
    {
        return Prop.ForAll(Arb.Default.Guid(), NonReadyCommitStatusesArb, (runGuid, status) =>
        {
            string runId = runGuid.ToString("N");

            ArchitectureRun runModel = new()
            {
                RunId = runId,
                RequestId = "r",
                Status = status,
                CreatedUtc = DateTime.UtcNow,
            };

            (IRunRepository runRepo, IScopeContextProvider scopeProvider) = CreateRunAuthorityMocks(runModel);

            Mock<IActorContext> actor = new();
            actor.Setup(x => x.GetActor()).Returns("fsm-prop-tester");

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

            try
            {
                sut.CommitRunAsync(runId, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (ConflictException)
            {
                return true.ToProperty();
            }
            catch (InvalidOperationException)
            {
                return (status == ArchitectureRunStatus.TasksGenerated).ToProperty();
            }

            return false.ToProperty();
        });
    }

    private static (IRunRepository Repo, IScopeContextProvider Scope) CreateRunAuthorityMocks(ArchitectureRun run)
    {
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(AuthorityTestScope);
        Mock<IRunRepository> runRepo = new();

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
            ContextSnapshotId = null,
            GraphSnapshotId = run.GraphSnapshotId,
            FindingsSnapshotId = run.FindingsSnapshotId,
            GoldenManifestId = run.GoldenManifestId,
            DecisionTraceId = run.DecisionTraceId,
            ArtifactBundleId = run.ArtifactBundleId,
        };
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
                Mock.Of<IPreCommitGovernanceGate>(),
                Options.Create(new PreCommitGovernanceGateOptions()),
                Mock.Of<IAuditService>(),
                NoOpTrialFunnelCommitHook.Instance,
                NullLogger<ArchitectureRunCommitOrchestrator>.Instance));
    }
}
