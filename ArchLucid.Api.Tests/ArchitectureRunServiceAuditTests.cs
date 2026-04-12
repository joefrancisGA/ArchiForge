using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application;
using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Coordinator.Services;
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

namespace ArchLucid.Api.Tests;

/// <summary>
/// Tests for Architecture Run Service Audit.
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
            runRepository: runRepo.Object,
            scopeContextProvider: scopeProvider.Object,
            actorContext: actor.Object,
            baselineMutationAudit: audit.Object);

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
    public async Task CommitRun_RunNotFound_RecordsRunFailedThenThrows()
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
            runRepository: runRepo.Object,
            scopeContextProvider: scopeProvider.Object,
            actorContext: actor.Object,
            baselineMutationAudit: audit.Object);

        Func<Task> act = () => sut.CommitRunAsync("missing");

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
                NullLogger<ArchitectureRunExecuteOrchestrator>.Instance),
            new ArchitectureRunCommitOrchestrator(
                runRepository,
                scopeContextProvider,
                Mock.Of<IArchitectureRequestRepository>(),
                Mock.Of<IAgentTaskRepository>(),
                Mock.Of<IAgentResultRepository>(),
                Mock.Of<IAgentEvaluationRepository>(),
                Mock.Of<IAgentEvidencePackageRepository>(),
                Mock.Of<IDecisionEngineService>(),
                Mock.Of<IDecisionEngineV2>(),
                Mock.Of<IDecisionNodeRepository>(),
                Mock.Of<ICoordinatorGoldenManifestRepository>(),
                Mock.Of<ICoordinatorDecisionTraceRepository>(),
                actorContext,
                baselineMutationAudit,
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                NullLogger<ArchitectureRunCommitOrchestrator>.Instance));
    }
}
