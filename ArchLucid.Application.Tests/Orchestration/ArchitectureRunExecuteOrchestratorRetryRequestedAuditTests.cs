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
///     Durable <see cref="IAuditService.LogAsync" /> when execute is invoked for a run whose authority status is
///     <see cref="ArchitectureRunStatus.Failed" /> (retry signal before baseline <c>Architecture.RunStarted</c>).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ArchitectureRunExecuteOrchestratorRetryRequestedAuditTests
{
    private static readonly ScopeContext TestScope = new()
    {
        TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ProjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    };

    [SkippableFact]
    public async Task ExecuteRunAsync_when_run_failed_emits_retry_requested_before_failing_execute_path()
    {
        Guid runGuid = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        string runId = runGuid.ToString("N");

        RunRecord header = new()
        {
            RunId = runGuid,
            TenantId = TestScope.TenantId,
            WorkspaceId = TestScope.WorkspaceId,
            ScopeProjectId = TestScope.ProjectId,
            ProjectId = "default",
            ArchitectureRequestId = "req-retry-audit",
            LegacyRunStatus = nameof(ArchitectureRunStatus.Failed),
            CreatedUtc = DateTime.UtcNow,
        };

        ArchitectureRequest request = new()
        {
            RequestId = "req-retry-audit",
            Description = new string('x', 12),
            SystemName = "RetryAudit",
        };

        Mock<IRunRepository> runRepo = new();
        runRepo
            .Setup(r => r.GetByIdAsync(TestScope, runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(header);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);

        Mock<IArchitectureRequestRepository> requestRepo = new();
        requestRepo.Setup(r => r.GetByIdAsync(request.RequestId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        Mock<IAgentTaskRepository> taskRepo = new();
        taskRepo.Setup(t => t.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Mock<IAgentExecutor> executor = new();
        Mock<IAgentEvaluationService> evaluationService = new();
        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(r => r.GetByRunIdAsync(runId, It.IsAny<CancellationToken>(), null, null)).ReturnsAsync([]);

        Mock<IAgentEvaluationRepository> evalRepo = new();
        Mock<IAgentEvidencePackageRepository> evidenceRepo = new();

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

        AuditEvent? capturedRetry = null;
        Mock<IAuditService> auditService = new();
        auditService
            .Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((e, _) => capturedRetry = e)
            .Returns(Task.CompletedTask);

        Mock<IActorContext> actorContext = new();
        actorContext.Setup(a => a.GetActor()).Returns("retry-actor");

        Mock<IRequestContentSafetyPrecheck> contentSafety = new();
        contentSafety
            .Setup(p => p.EvaluateAsync(It.IsAny<ArchitectureRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestContentSafetyResult { IsAllowed = true });

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
            contentSafety.Object,
            NullLogger<ArchitectureRunExecuteOrchestrator>.Instance);

        Func<Task> act = async () => await sut.ExecuteRunAsync(runId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No tasks found*");

        capturedRetry.Should().NotBeNull();
        capturedRetry!.EventType.Should().Be(AuditEventTypes.Run.RetryRequested);
        capturedRetry.RunId.Should().Be(runGuid);
        capturedRetry.TenantId.Should().Be(TestScope.TenantId);
        capturedRetry.WorkspaceId.Should().Be(TestScope.WorkspaceId);
        capturedRetry.ProjectId.Should().Be(TestScope.ProjectId);
        capturedRetry.DataJson.Should().Contain(runId);
        capturedRetry.DataJson.Should().Contain(nameof(ArchitectureRunStatus.Failed));

        auditService.Verify(
            a => a.LogAsync(It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.Run.RetryRequested), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [SkippableFact]
    public async Task ExecuteRunAsync_when_retry_requested_audit_fails_repeatedly_still_surfaces_execute_validation_error()
    {
        Guid runGuid = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        string runId = runGuid.ToString("N");

        RunRecord header = new()
        {
            RunId = runGuid,
            TenantId = TestScope.TenantId,
            WorkspaceId = TestScope.WorkspaceId,
            ScopeProjectId = TestScope.ProjectId,
            ProjectId = "default",
            ArchitectureRequestId = "req-retry-audit-sql",
            LegacyRunStatus = nameof(ArchitectureRunStatus.Failed),
            CreatedUtc = DateTime.UtcNow,
        };

        ArchitectureRequest request = new()
        {
            RequestId = "req-retry-audit-sql",
            Description = new string('x', 12),
            SystemName = "RetryAuditSql",
        };

        Mock<IRunRepository> runRepo = new();
        runRepo
            .Setup(r => r.GetByIdAsync(TestScope, runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(header);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(TestScope);

        Mock<IArchitectureRequestRepository> requestRepo = new();
        requestRepo.Setup(r => r.GetByIdAsync(request.RequestId, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        Mock<IAgentTaskRepository> taskRepo = new();
        taskRepo.Setup(t => t.GetByRunIdAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Mock<IAgentExecutor> executor = new();
        Mock<IAgentEvaluationService> evaluationService = new();
        Mock<IAgentResultRepository> resultRepo = new();
        resultRepo.Setup(r => r.GetByRunIdAsync(runId, It.IsAny<CancellationToken>(), null, null)).ReturnsAsync([]);

        Mock<IAgentEvaluationRepository> evalRepo = new();
        Mock<IAgentEvidencePackageRepository> evidenceRepo = new();

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
            .ThrowsAsync(new InvalidOperationException("audit sql unavailable"));

        Mock<IActorContext> actorContext = new();
        actorContext.Setup(a => a.GetActor()).Returns("retry-actor");

        Mock<IRequestContentSafetyPrecheck> contentSafety = new();
        contentSafety
            .Setup(p => p.EvaluateAsync(It.IsAny<ArchitectureRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestContentSafetyResult { IsAllowed = true });

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
            contentSafety.Object,
            NullLogger<ArchitectureRunExecuteOrchestrator>.Instance);

        Func<Task> act = async () => await sut.ExecuteRunAsync(runId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No tasks found*");

        auditService.Verify(
            a => a.LogAsync(It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.Run.RetryRequested), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }
}
