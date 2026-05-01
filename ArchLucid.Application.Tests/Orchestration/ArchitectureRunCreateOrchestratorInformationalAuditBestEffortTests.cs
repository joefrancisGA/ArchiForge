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
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Orchestration;

/// <summary>
///     Informational durable audit rows after successful run persistence must not fail <see cref="ArchitectureRunCreateOrchestrator.CreateRunAsync" />.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ArchitectureRunCreateOrchestratorInformationalAuditBestEffortTests
{
    private static readonly ScopeContext CreateAuditScope = new()
    {
        TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
    };

    [SkippableFact]
    public async Task CreateRunAsync_when_informational_durable_audit_fails_returns_success()
    {
        Mock<IArchitectureRunAuthorityCoordination> coordination = new();
        Mock<IArchitectureRequestRepository> requestRepo = new();
        Mock<IRunRepository> runRepo = new();
        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(CreateAuditScope);
        Mock<IEvidenceBundleRepository> evidenceRepo = new();
        Mock<IAgentTaskRepository> taskRepo = new();
        Mock<IArchitectureRunIdempotencyRepository> idempotencyRepo = new();
        Mock<IActorContext> actorContext = new();
        actorContext.Setup(a => a.GetActor()).Returns("create-audit-actor");
        Mock<IBaselineMutationAuditService> baselineAudit = new();
        baselineAudit
            .Setup(
                b => b.RecordAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IAuditService> auditService = new();
        auditService
            .Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("audit sql down"));

        Mock<IUsageMeteringService> metering = new();
        metering
            .Setup(m => m.RecordAsync(It.IsAny<UsageEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        requestRepo
            .Setup(r => r.CreateAsync(It.IsAny<ArchitectureRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        evidenceRepo
            .Setup(r => r.CreateAsync(It.IsAny<EvidenceBundle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        taskRepo
            .Setup(r => r.CreateManyAsync(It.IsAny<IEnumerable<AgentTask>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        string runId = Guid.NewGuid().ToString("N");
        ArchitectureRun run = new()
        {
            RunId = runId,
            RequestId = "req-audit-best-effort",
            Status = ArchitectureRunStatus.Created,
            CreatedUtc = DateTime.UtcNow,
        };
        EvidenceBundle bundle = new() { EvidenceBundleId = "eb-audit-be" };
        List<AgentTask> tasks =
        [
            new()
            {
                TaskId = "t-audit",
                RunId = runId,
                EvidenceBundleRef = "eb-audit-be",
                AgentType = AgentType.Topology,
                Objective = "obj",
                Status = AgentTaskStatus.Created,
                CreatedUtc = DateTime.UtcNow,
            },
        ];

        CoordinationResult coordinationResult = new()
        {
            Run = run,
            EvidenceBundle = bundle,
            Tasks = tasks,
        };

        coordination
            .Setup(c => c.CreateRunAsync(It.IsAny<ArchitectureRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(coordinationResult);

        ArchitectureRequest request = new()
        {
            RequestId = "req-audit-best-effort",
            Description = new string('y', 12),
            SystemName = "SysAuditBe",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
        };

        ArchitectureRunCreateOrchestrator sut = new(
            coordination.Object,
            requestRepo.Object,
            runRepo.Object,
            scopeProvider.Object,
            evidenceRepo.Object,
            taskRepo.Object,
            idempotencyRepo.Object,
            actorContext.Object,
            baselineAudit.Object,
            auditService.Object,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            metering.Object,
            new NoOpDistributedCreateRunIdempotencyLock(),
            Options.Create(new ArchitectureRunCreateOptions()),
            TimeProvider.System,
            NullLogger<ArchitectureRunCreateOrchestrator>.Instance);

        CreateRunResult result = await sut.CreateRunAsync(request, null, CancellationToken.None);

        result.Run.RunId.Should().Be(runId);
        result.Tasks.Should().HaveCount(1);
        result.IdempotentReplay.Should().BeFalse();

        auditService.Verify(
            a => a.LogAsync(It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.RequestCreated), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        auditService.Verify(
            a => a.LogAsync(It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.RequestLocked), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }
}
