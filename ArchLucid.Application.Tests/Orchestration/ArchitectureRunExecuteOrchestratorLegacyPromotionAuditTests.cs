using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
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
///     Durable <see cref="IAuditService.LogAsync" /> when execute promotes <c>dbo.Runs.LegacyRunStatus</c> to
///     <see cref="ArchitectureRunStatus.ReadyForCommit" /> (ADR-0012).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ArchitectureRunExecuteOrchestratorLegacyPromotionAuditTests
{
    private static readonly ScopeContext TestScope = new()
    {
        TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    };

    [Fact]
    public async Task ExecuteRunAsync_after_persist_emits_durable_audit_when_legacy_status_promoted_to_ready_for_commit()
    {
        Guid runGuid = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        string runId = runGuid.ToString("N");

        RunRecord header = new()
        {
            RunId = runGuid,
            TenantId = TestScope.TenantId,
            WorkspaceId = TestScope.WorkspaceId,
            ScopeProjectId = TestScope.ProjectId,
            ProjectId = "default",
            ArchitectureRequestId = "req-promote-audit",
            LegacyRunStatus = nameof(ArchitectureRunStatus.TasksGenerated),
            CreatedUtc = DateTime.UtcNow,
        };

        ArchitectureRequest request = new()
        {
            RequestId = "req-promote-audit",
            Description = new string('x', 12),
            SystemName = "PromoteAudit",
        };

        Mock<IRunRepository> runRepo = new();
        runRepo
            .Setup(r => r.GetByIdAsync(TestScope, runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(header);

        runRepo
            .Setup(r => r.UpdateAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);

        Mock<IArchitectureRequestRepository> requestRepo = new();
        requestRepo.Setup(r => r.GetByIdAsync(request.RequestId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        Mock<IAgentTaskRepository> taskRepo = new();
        taskRepo
            .Setup(t => t.GetByRunIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new AgentTask
                {
                    RunId = runId,
                    AgentType = AgentType.Topology,
                    TaskId = Guid.NewGuid().ToString("N"),
                },
            ]);

        IReadOnlyList<AgentResult> fourResults = BuildFourResults(runId);

        Mock<IAgentExecutor> executor = new();
        executor
            .Setup(
                e => e.ExecuteAsync(
                    runId,
                    request,
                    It.IsAny<AgentEvidencePackage>(),
                    It.IsAny<IReadOnlyList<AgentTask>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(fourResults);

        Mock<IAgentEvaluationService> evaluationService = new();
        evaluationService
            .Setup(
                e => e.EvaluateAsync(
                    runId,
                    request,
                    It.IsAny<AgentEvidencePackage>(),
                    It.IsAny<IReadOnlyCollection<AgentTask>>(),
                    It.IsAny<IReadOnlyCollection<AgentResult>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(r => r.GetByRunIdAsync(runId, It.IsAny<CancellationToken>(), null, null)).ReturnsAsync([]);
        resultRepo
            .Setup(r => r.CreateManyAsync(It.IsAny<IReadOnlyList<AgentResult>>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

        Mock<IAgentEvaluationRepository> evalRepo = new();
        evalRepo
            .Setup(
                r => r.CreateManyAsync(
                    It.IsAny<IReadOnlyCollection<AgentEvaluation>>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null))
            .Returns(Task.CompletedTask);

        Mock<IAgentEvidencePackageRepository> evidenceRepo = new();
        evidenceRepo
            .Setup(r => r.CreateAsync(It.IsAny<AgentEvidencePackage>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);

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

        AuditEvent? captured = null;
        Mock<IAuditService> auditService = new();
        auditService
            .Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        Mock<IActorContext> actorContext = new();
        actorContext.Setup(a => a.GetActor()).Returns("promote-actor");

        ArchitectureRunExecuteOrchestrator sut = new(
            runRepo.Object,
            scopeProvider.Object,
            requestRepo.Object,
            taskRepo.Object,
            executor.Object,
            evaluationService.Object,
            resultRepo.Object,
            evalRepo.Object,
            evidenceRepo.Object,
            new DefaultEvidenceBuilder(),
            actorContext.Object,
            baselineAudit.Object,
            auditService.Object,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            new NoOpAgentOutputTraceEvaluationHook(),
            NullLogger<ArchitectureRunExecuteOrchestrator>.Instance);

        await sut.ExecuteRunAsync(runId);

        captured.Should().NotBeNull();
        captured!.EventType.Should().Be(AuditEventTypes.RunLegacyReadyForCommitPromoted);
        captured.RunId.Should().Be(runGuid);
        captured.TenantId.Should().Be(TestScope.TenantId);
        captured.DataJson.Should().Contain(nameof(ArchitectureRunStatus.TasksGenerated));
        captured.DataJson.Should().Contain(nameof(ArchitectureRunStatus.ReadyForCommit));

        auditService.Verify(
            a => a.LogAsync(It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.RunLegacyReadyForCommitPromoted), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static List<AgentResult> BuildFourResults(string runId)
    {
        AgentType[] types = [AgentType.Topology, AgentType.Cost, AgentType.Compliance, AgentType.Critic];

        return types
            .Select(
                t => new AgentResult
                {
                    RunId = runId,
                    AgentType = t,
                    TaskId = $"{t}-{Guid.NewGuid():N}",
                    Claims = ["c"],
                    EvidenceRefs = ["e"],
                    Confidence = 0.9,
                })
            .ToList();
    }
}
