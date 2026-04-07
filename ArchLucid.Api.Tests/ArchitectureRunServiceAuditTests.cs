using ArchLucid.AgentSimulator.Services;
using ArchLucid.Application;
using ArchLucid.Application.Architecture;
using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Coordinator.Services;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Decisioning.Merge;
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
    [Fact]
    public async Task ExecuteRun_RunNotFound_RecordsRunFailedThenThrows()
    {
        Mock<IArchitectureRunRepository> runRepo = new();
        runRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRun?)null);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("audit-actor");

        Mock<IBaselineMutationAuditService> audit = new();

        ArchitectureRunService sut = CreateRunService(
            runRepository: runRepo.Object,
            actorContext: actor.Object,
            baselineMutationAudit: audit.Object);

        Func<Task> act = () => sut.ExecuteRunAsync("missing");

        await act.Should().ThrowAsync<RunNotFoundException>();

        audit.Verify(
            a => a.RecordAsync(
                AuditEventTypes.Architecture.RunFailed,
                "audit-actor",
                "missing",
                "Run not found.",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitRun_RunNotFound_RecordsRunFailedThenThrows()
    {
        Mock<IArchitectureRunRepository> runRepo = new();
        runRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRun?)null);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("audit-actor");

        Mock<IBaselineMutationAuditService> audit = new();

        ArchitectureRunService sut = CreateRunService(
            runRepository: runRepo.Object,
            actorContext: actor.Object,
            baselineMutationAudit: audit.Object);

        Func<Task> act = () => sut.CommitRunAsync("missing");

        await act.Should().ThrowAsync<RunNotFoundException>();

        audit.Verify(
            a => a.RecordAsync(
                AuditEventTypes.Architecture.RunFailed,
                "audit-actor",
                "missing",
                "Run not found.",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ArchitectureRunService CreateRunService(
        IArchitectureRunRepository runRepository,
        IActorContext actorContext,
        IBaselineMutationAuditService baselineMutationAudit)
    {
        return new ArchitectureRunService(
            new ArchitectureRunCreateOrchestrator(
                Mock.Of<ICoordinatorService>(),
                Mock.Of<IArchitectureRequestRepository>(),
                runRepository,
                Mock.Of<IEvidenceBundleRepository>(),
                Mock.Of<IAgentTaskRepository>(),
                Mock.Of<IArchitectureRunIdempotencyRepository>(),
                actorContext,
                baselineMutationAudit,
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                NullLogger<ArchitectureRunCreateOrchestrator>.Instance),
            new ArchitectureRunExecuteOrchestrator(
                runRepository,
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
