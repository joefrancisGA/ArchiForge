using ArchiForge.AgentSimulator.Services;
using ArchiForge.Application;
using ArchiForge.Application.Common;
using ArchiForge.Application.Decisions;
using ArchiForge.Application.Evidence;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Coordinator.Services;
using ArchiForge.Persistence.Data.Repositories;
using ArchiForge.DecisionEngine.Services;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchiForge.Api.Tests;

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
            Mock.Of<ICoordinatorService>(),
            Mock.Of<IAgentExecutor>(),
            Mock.Of<IDecisionEngineService>(),
            Mock.Of<IAgentEvaluationService>(),
            Mock.Of<IAgentEvaluationRepository>(),
            Mock.Of<IDecisionEngineV2>(),
            Mock.Of<IDecisionNodeRepository>(),
            Mock.Of<IEvidenceBuilder>(),
            Mock.Of<IArchitectureRequestRepository>(),
            runRepository,
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IAgentResultRepository>(),
            Mock.Of<IGoldenManifestRepository>(),
            Mock.Of<IEvidenceBundleRepository>(),
            Mock.Of<IDecisionTraceRepository>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            Mock.Of<IArchitectureRunIdempotencyRepository>(),
            actorContext,
            baselineMutationAudit,
            Mock.Of<ILogger<ArchitectureRunService>>());
    }
}
