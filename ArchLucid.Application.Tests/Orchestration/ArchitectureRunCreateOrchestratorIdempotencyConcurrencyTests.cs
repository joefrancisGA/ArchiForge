using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Application.Common;
using ArchLucid.Application.Runs;
using ArchLucid.Application.Runs.Coordination;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Concurrency;
using ArchLucid.Core.Metering;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Orchestration;

/// <summary>
/// Concurrent <see cref="ArchitectureRunCreateOrchestrator.CreateRunAsync"/> calls with the same idempotency key
/// must not invoke the coordinator more than once per process (see class remarks on multi-replica limits).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ArchitectureRunCreateOrchestratorIdempotencyConcurrencyTests
{
    private static readonly ScopeContext TestScope = new()
    {
        TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
    };

    [SkippableFact]
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public async Task Parallel_create_with_same_idempotency_key_invokes_coordinator_once()
    {
        Guid tenantId = TestScope.TenantId;
        Guid workspaceId = TestScope.WorkspaceId;
        Guid projectId = TestScope.ProjectId;
        byte[] keyHash = new byte[32];
        Array.Fill(keyHash, (byte)11);
        byte[] fingerprint = new byte[32];
        Array.Fill(fingerprint, (byte)22);
        CreateRunIdempotencyState idempotency = new(tenantId, workspaceId, projectId, keyHash, fingerprint);

        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-conc-" + Guid.NewGuid().ToString("N");

        ArchitectureRequest request = new()
        {
            RequestId = requestId,
            SystemName = "ConcSys",
            Environment = "dev",
            CloudProvider = CloudProvider.Azure,
        };

        CoordinationResult coordinationOutcome = new()
        {
            Run = new ArchitectureRun
            {
                RunId = runId,
                RequestId = requestId,
                Status = ArchitectureRunStatus.TasksGenerated,
                CreatedUtc = DateTime.UtcNow,
            },
            EvidenceBundle = new EvidenceBundle { EvidenceBundleId = "eb-conc" },
            Tasks =
            [
                new AgentTask
                {
                    TaskId = "t-conc",
                    RunId = runId,
                    EvidenceBundleRef = "eb-conc",
                    AgentType = AgentType.Topology,
                    Objective = "o",
                    Status = AgentTaskStatus.Created,
                    CreatedUtc = DateTime.UtcNow,
                },
            ],
        };

        int coordinatorInvocations = 0;
        Mock<IArchitectureRunAuthorityCoordination> coordinationMock = new();
        coordinationMock
            .Setup(c => c.CreateRunAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref coordinatorInvocations);
                return coordinationOutcome;
            });

        string? publishedWinnerRunId = null;

        Mock<IArchitectureRunIdempotencyRepository> idempotencyRepository = new();
        idempotencyRepository
            .Setup(x => x.TryGetAsync(tenantId, workspaceId, projectId, keyHash, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                string? w = Volatile.Read(ref publishedWinnerRunId);
                if (w is null)
                    return Task.FromResult<ArchitectureRunIdempotencyLookup?>(null);

                return Task.FromResult<ArchitectureRunIdempotencyLookup?>(
                    new ArchitectureRunIdempotencyLookup
                    {
                        RunId = w,
                        RequestFingerprint = fingerprint,
                    });
            });

        idempotencyRepository
            .Setup(x => x.TryInsertAsync(
                tenantId,
                workspaceId,
                projectId,
                keyHash,
                fingerprint,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>()))
            .Returns(
                (
                    Guid tenant,
                    Guid workspace,
                    Guid project,
                    byte[] innerKeyHash,
                    byte[] innerFingerprint,
                    string rid,
                    CancellationToken ct,
                    IDbConnection? conn,
                    IDbTransaction? tx) =>
                {
                    Volatile.Write(ref publishedWinnerRunId, rid);
                    return Task.FromResult(true);
                });

        ArchitectureRun runModel = coordinationOutcome.Run;
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);

        Mock<IRunRepository> runRepo = new();
        Guid runGuid = Guid.ParseExact(runModel.RunId, "N");
        RunRecord rec = new()
        {
            RunId = runGuid,
            TenantId = TestScope.TenantId,
            WorkspaceId = TestScope.WorkspaceId,
            ScopeProjectId = TestScope.ProjectId,
            ProjectId = "conc-proj",
            ArchitectureRequestId = runModel.RequestId,
            LegacyRunStatus = runModel.Status.ToString(),
            CreatedUtc = runModel.CreatedUtc,
            CompletedUtc = runModel.CompletedUtc,
            CurrentManifestVersion = runModel.CurrentManifestVersion,
        };
        runRepo.Setup(r => r.GetByIdAsync(TestScope, runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rec);

        Mock<IAgentTaskRepository> taskRepository = new();
        taskRepository
            .Setup(x => x.GetByRunIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coordinationOutcome.Tasks);

        Mock<IEvidenceBundleRepository> evidenceBundleRepository = new();
        evidenceBundleRepository
            .Setup(x => x.GetByIdAsync("eb-conc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(coordinationOutcome.EvidenceBundle);

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("test-actor");

        ArchitectureRunCreateOrchestrator sut = new(
            coordinationMock.Object,
            Mock.Of<IArchitectureRequestRepository>(),
            runRepo.Object,
            scopeProvider.Object,
            evidenceBundleRepository.Object,
            taskRepository.Object,
            idempotencyRepository.Object,
            actor.Object,
            Mock.Of<IBaselineMutationAuditService>(),
            Mock.Of<IAuditService>(),
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            Mock.Of<IUsageMeteringService>(),
            new NoOpDistributedCreateRunIdempotencyLock(),
            Microsoft.Extensions.Options.Options.Create(new ArchitectureRunCreateOptions()),
            TimeProvider.System,
            new DefaultRequestContentSafetyPrecheck(),
            NullLogger<ArchitectureRunCreateOrchestrator>.Instance);

        const int parallel = 64;
        Task<CreateRunResult>[] tasks = Enumerable
            .Range(0, parallel)
            .Select(_ => sut.CreateRunAsync(request, idempotency, CancellationToken.None))
            .ToArray();

        CreateRunResult[] results = await Task.WhenAll(tasks);

        coordinatorInvocations.Should().Be(1);
        coordinationMock.Verify(c => c.CreateRunAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        results.Should().HaveCount(parallel);
        results.Select(r => r.Run.RunId).Distinct().Should().ContainSingle().Which.Should().Be(runId);
        results.Count(r => r.IdempotentReplay).Should().Be(parallel - 1);
    }

    /// <summary>
    /// Distinct idempotency keys must not collapse: each parallel create should invoke coordination once.
    /// </summary>
    [SkippableFact]
    public async Task Parallel_create_with_distinct_idempotency_keys_invokes_coordinator_each_time()
    {
        Guid tenantId = TestScope.TenantId;
        Guid workspaceId = TestScope.WorkspaceId;
        Guid projectId = TestScope.ProjectId;
        byte[] fingerprint = new byte[32];
        Array.Fill(fingerprint, (byte)44);

        int coordinatorInvocations = 0;
        Mock<IArchitectureRunAuthorityCoordination> coordination = new();
        coordination
            .Setup(c => c.CreateRunAsync(It.IsAny<ArchitectureRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (ArchitectureRequest req, CancellationToken cancellationToken) =>
                {
                    Interlocked.Increment(ref coordinatorInvocations);

                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);
                    string runId = Guid.NewGuid().ToString("N");

                    return new CoordinationResult
                    {
                        Run = new ArchitectureRun
                        {
                            RunId = runId,
                            RequestId = req.RequestId,
                            Status = ArchitectureRunStatus.TasksGenerated,
                            CreatedUtc = DateTime.UtcNow,
                        },
                        EvidenceBundle = new EvidenceBundle { EvidenceBundleId = "eb-" + runId },
                        Tasks = [],
                    };
                });

        Mock<IArchitectureRunIdempotencyRepository> idempotencyRepository = new();
        idempotencyRepository
            .Setup(x => x.TryGetAsync(tenantId, workspaceId, projectId, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunIdempotencyLookup?)null);

        idempotencyRepository
            .Setup(x => x.TryInsertAsync(
                tenantId,
                workspaceId,
                projectId,
                It.IsAny<byte[]>(),
                fingerprint,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IDbConnection>(),
                It.IsAny<IDbTransaction>()))
            .ReturnsAsync(true);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);

        Mock<IRunRepository> runRepo = new();
        Mock<IAgentTaskRepository> taskRepository = new();
        Mock<IEvidenceBundleRepository> evidenceBundleRepository = new();
        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("test-actor");

        Mock<IArchitectureRequestRepository> requestRepository = new();
        requestRepository
            .Setup(r => r.CreateAsync(It.IsAny<ArchitectureRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        evidenceBundleRepository
            .Setup(e => e.CreateAsync(It.IsAny<EvidenceBundle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ArchitectureRunCreateOrchestrator sut = new(
            coordination.Object,
            requestRepository.Object,
            runRepo.Object,
            scopeProvider.Object,
            evidenceBundleRepository.Object,
            taskRepository.Object,
            idempotencyRepository.Object,
            actor.Object,
            Mock.Of<IBaselineMutationAuditService>(),
            Mock.Of<IAuditService>(),
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            Mock.Of<IUsageMeteringService>(),
            new NoOpDistributedCreateRunIdempotencyLock(),
            Microsoft.Extensions.Options.Options.Create(new ArchitectureRunCreateOptions()),
            TimeProvider.System,
            new DefaultRequestContentSafetyPrecheck(),
            NullLogger<ArchitectureRunCreateOrchestrator>.Instance);

        const int parallel = 64;
        ConcurrentBag<string> runIds = [];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, parallel),
            new ParallelOptions { MaxDegreeOfParallelism = parallel },
            async (i, ct) =>
            {
                byte[] keyHash = new byte[32];
                keyHash[0] = (byte)i;
                keyHash[1] = (byte)(i >> 8);
                CreateRunIdempotencyState idempotency = new(tenantId, workspaceId, projectId, keyHash, fingerprint);

                ArchitectureRequest request = new()
                {
                    RequestId = "req-dist-" + i.ToString("x8"),
                    SystemName = "DistSys",
                    Environment = "dev",
                    CloudProvider = CloudProvider.Azure,
                };

                CreateRunResult result = await sut.CreateRunAsync(request, idempotency, ct);
                runIds.Add(result.Run.RunId);
            });

        coordinatorInvocations.Should().Be(parallel);
        runIds.Should().HaveCount(parallel);
        runIds.Distinct().Should().HaveCount(parallel);
    }
}
