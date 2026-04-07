using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Application;
using ArchLucid.Application.Architecture;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Coordinator.Services;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.TestSupport;
using ArchLucid.Decisioning.Merge;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests;

/// <summary>
/// <see cref="ArchitectureRunService.CreateRunAsync"/> idempotency: replay vs fingerprint conflict without coordinating a new run.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ArchitectureRunServiceCreateRunIdempotencyTests
{
    [Fact]
    public async Task CreateRunAsync_when_idempotency_replay_matches_skips_coordinator_and_marks_replay()
    {
        Mock<ICoordinatorService> coordinator = new();
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        byte[] keyHash = new byte[32];
        Array.Fill(keyHash, (byte)7);
        byte[] fingerprint = new byte[32];
        Array.Fill(fingerprint, (byte)8);
        CreateRunIdempotencyState idempotency = new(tenantId, workspaceId, projectId, keyHash, fingerprint);

        string runId = Guid.NewGuid().ToString("N");
        Mock<IArchitectureRunIdempotencyRepository> idempotencyRepository = new();
        idempotencyRepository
            .Setup(x => x.TryGetAsync(tenantId, workspaceId, projectId, keyHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchitectureRunIdempotencyLookup
            {
                RunId = runId,
                RequestFingerprint = fingerprint,
            });

        ArchitectureRun run = new() { RunId = runId, RequestId = "prior-req" };
        EvidenceBundle bundle = new() { EvidenceBundleId = "eb-contract" };
        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = "t1",
                RunId = runId,
                EvidenceBundleRef = "eb-contract",
                AgentType = AgentType.Topology,
                Objective = "o",
                Status = AgentTaskStatus.Created,
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        Mock<IArchitectureRunRepository> runRepository = new();
        runRepository
            .Setup(x => x.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        Mock<IAgentTaskRepository> taskRepository = new();
        taskRepository
            .Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        Mock<IEvidenceBundleRepository> evidenceBundleRepository = new();
        evidenceBundleRepository
            .Setup(x => x.GetByIdAsync("eb-contract", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bundle);

        Mock<IActorContext> actorContext = new();
        actorContext.Setup(x => x.GetActor()).Returns("test-actor");

        ArchitectureRunService sut = CreateSut(
            coordinator.Object,
            idempotencyRepository.Object,
            runRepository.Object,
            taskRepository.Object,
            evidenceBundleRepository.Object,
            actorContext.Object);

        ArchitectureRequest request = new()
        {
            RequestId = "new-req",
            SystemName = "Sys",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
        };

        CreateRunResult result = await sut.CreateRunAsync(request, idempotency, CancellationToken.None);

        result.IdempotentReplay.Should().BeTrue();
        result.Run.RunId.Should().Be(runId);
        result.EvidenceBundle.EvidenceBundleId.Should().Be("eb-contract");
        result.Tasks.Should().HaveCount(1);

        coordinator.Verify(
            x => x.CreateRunAsync(It.IsAny<ArchitectureRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateRunAsync_when_fingerprint_mismatch_throws_ConflictException_without_coordination()
    {
        Mock<ICoordinatorService> coordinator = new();
        Guid tenantId = Guid.NewGuid();
        byte[] keyHash = new byte[32];
        Array.Fill(keyHash, (byte)3);
        byte[] newFingerprint = new byte[32];
        Array.Fill(newFingerprint, (byte)9);
        byte[] storedFingerprint = new byte[32];
        Array.Fill(storedFingerprint, (byte)4);
        CreateRunIdempotencyState idempotency = new(tenantId, Guid.NewGuid(), Guid.NewGuid(), keyHash, newFingerprint);

        Mock<IArchitectureRunIdempotencyRepository> idempotencyRepository = new();
        idempotencyRepository
            .Setup(x => x.TryGetAsync(tenantId, It.IsAny<Guid>(), It.IsAny<Guid>(), keyHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchitectureRunIdempotencyLookup
            {
                RunId = Guid.NewGuid().ToString("N"),
                RequestFingerprint = storedFingerprint,
            });

        Mock<IActorContext> actorContext = new();
        actorContext.Setup(x => x.GetActor()).Returns("test-actor");

        ArchitectureRunService sut = CreateSut(
            coordinator.Object,
            idempotencyRepository.Object,
            Mock.Of<IArchitectureRunRepository>(),
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IEvidenceBundleRepository>(),
            actorContext.Object);

        ArchitectureRequest request = new()
        {
            RequestId = "req",
            SystemName = "S",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
        };

        Func<Task> act = async () => await sut.CreateRunAsync(request, idempotency, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();

        coordinator.Verify(
            x => x.CreateRunAsync(It.IsAny<ArchitectureRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ArchitectureRunService CreateSut(
        ICoordinatorService coordinator,
        IArchitectureRunIdempotencyRepository architectureRunIdempotencyRepository,
        IArchitectureRunRepository runRepository,
        IAgentTaskRepository taskRepository,
        IEvidenceBundleRepository evidenceBundleRepository,
        IActorContext actorContext)
    {
        IBaselineMutationAuditService audit = Mock.Of<IBaselineMutationAuditService>();

        return new ArchitectureRunService(
            new ArchitectureRunCreateOrchestrator(
                coordinator,
                Mock.Of<IArchitectureRequestRepository>(),
                runRepository,
                evidenceBundleRepository,
                taskRepository,
                architectureRunIdempotencyRepository,
                actorContext,
                audit,
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                NullLogger<ArchitectureRunCreateOrchestrator>.Instance),
            new ArchitectureRunExecuteOrchestrator(
                runRepository,
                Mock.Of<IArchitectureRequestRepository>(),
                taskRepository,
                Mock.Of<AgentSimulator.Services.IAgentExecutor>(),
                Mock.Of<IAgentEvaluationService>(),
                Mock.Of<IAgentResultRepository>(),
                Mock.Of<IAgentEvaluationRepository>(),
                Mock.Of<IAgentEvidencePackageRepository>(),
                Mock.Of<IEvidenceBuilder>(),
                actorContext,
                audit,
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                NullLogger<ArchitectureRunExecuteOrchestrator>.Instance),
            new ArchitectureRunCommitOrchestrator(
                runRepository,
                Mock.Of<IArchitectureRequestRepository>(),
                taskRepository,
                Mock.Of<IAgentResultRepository>(),
                Mock.Of<IAgentEvaluationRepository>(),
                Mock.Of<IAgentEvidencePackageRepository>(),
                Mock.Of<IDecisionEngineService>(),
                Mock.Of<IDecisionEngineV2>(),
                Mock.Of<IDecisionNodeRepository>(),
                Mock.Of<ICoordinatorGoldenManifestRepository>(),
                Mock.Of<ICoordinatorDecisionTraceRepository>(),
                actorContext,
                audit,
                ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
                NullLogger<ArchitectureRunCommitOrchestrator>.Instance));
    }
}
