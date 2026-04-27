using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Application;
using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Agents;
using ArchLucid.Coordinator.Services;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Concurrency;
using ArchLucid.Core.Metering;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Run Service Audit.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ArchitectureRunServiceAuditTests
{
    private static readonly ScopeContext AuthorityTestScope = new()
    {
        TenantId = Guid.Parse("99999999-9999-9999-9999-999999999991"),
        WorkspaceId = Guid.Parse("99999999-9999-9999-9999-999999999992"),
        ProjectId = Guid.Parse("99999999-9999-9999-9999-999999999993")
    };

    [Fact]
    public async Task ExecuteRun_RunNotFound_RecordsRunFailedThenThrows()
    {
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(AuthorityTestScope);
        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(r => r.GetByIdAsync(AuthorityTestScope, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("audit-actor");

        Mock<IBaselineMutationAuditService> audit = new();

        ArchitectureRunService sut = CreateRunService(
            runRepo.Object,
            scopeProvider.Object,
            actor.Object,
            audit.Object);

        Func<Task> act = () => sut.ExecuteRunAsync("missing");

        await act.Should().ThrowAsync<RunNotFoundException>();

        audit.Verify(
            a => a.RecordAsync(
                AuditEventTypes.Baseline.Architecture.RunFailed,
                "audit-actor",
                "missing",
                "Run not found.",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitRun_PropagatesRunNotFoundFromOrchestrator()
    {
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(AuthorityTestScope);
        Mock<IRunRepository> runRepo = new();
        runRepo.Setup(r => r.GetByIdAsync(AuthorityTestScope, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("audit-actor");

        Mock<IBaselineMutationAuditService> audit = new();

        // ADR 0030 PR A3 (2026-04-24): the legacy ArchitectureRunCommitOrchestrator audit-on-not-found
        // behavior moved to AuthorityDrivenArchitectureRunCommitOrchestrator and is covered by its own
        // tests. ArchitectureRunService is now only responsible for delegating CommitRunAsync; we assert
        // the exception still propagates to the caller.
        ArchitectureRunService sut = CreateRunService(
            runRepo.Object,
            scopeProvider.Object,
            actor.Object,
            audit.Object);

        Func<Task> act = () => sut.CommitRunAsync("missing");

        await act.Should().ThrowAsync<RunNotFoundException>();
    }

    private static ArchitectureRunService CreateRunService(
        IRunRepository runRepository,
        IScopeContextProvider scopeContextProvider,
        IActorContext actorContext,
        IBaselineMutationAuditService baselineMutationAudit)
    {
        return new ArchitectureRunService(
            new ArchitectureRunCreateOrchestrator(
                Mock.Of<ICoordinatorService>(),
                Mock.Of<IArchitectureRequestRepository>(),
                runRepository,
                scopeContextProvider,
                Mock.Of<IEvidenceBundleRepository>(),
                Mock.Of<IAgentTaskRepository>(),
                Mock.Of<IArchitectureRunIdempotencyRepository>(),
                actorContext,
                baselineMutationAudit,
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                Mock.Of<IUsageMeteringService>(),
                new NoOpDistributedCreateRunIdempotencyLock(),
                Options.Create(new ArchitectureRunCreateOptions()),
                TimeProvider.System,
                NullLogger<ArchitectureRunCreateOrchestrator>.Instance),
            new ArchitectureRunExecuteOrchestrator(
                runRepository,
                scopeContextProvider,
                Mock.Of<IArchitectureRequestRepository>(),
                Mock.Of<IAgentTaskRepository>(),
                Mock.Of<IAgentExecutor>(),
                Mock.Of<IAgentEvaluationService>(),
                Mock.Of<IAgentResultRepository>(),
                Mock.Of<IAgentEvaluationRepository>(),
                Mock.Of<IAgentEvidencePackageRepository>(),
                Mock.Of<IEvidenceBuilder>(),
                actorContext,
                baselineMutationAudit,
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                new NoOpAgentOutputTraceEvaluationHook(),
                NullLogger<ArchitectureRunExecuteOrchestrator>.Instance),
            // ADR 0030 PR A3 (2026-04-24): the legacy ArchitectureRunCommitOrchestrator concrete was deleted.
            // CommitRun audit behavior now lives inside AuthorityDrivenArchitectureRunCommitOrchestrator and is
            // covered by its own audit tests; here we simply propagate the run-not-found exception.
            BuildCommitOrchestratorThatThrowsRunNotFound());
    }

    private static IArchitectureRunCommitOrchestrator BuildCommitOrchestratorThatThrowsRunNotFound()
    {
        Mock<IArchitectureRunCommitOrchestrator> commit = new();
        commit
            .Setup(c => c.CommitRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((runId, _) => throw new RunNotFoundException(runId));

        return commit.Object;
    }
}
