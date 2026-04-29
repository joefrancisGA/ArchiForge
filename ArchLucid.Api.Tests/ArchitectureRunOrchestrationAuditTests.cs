using ArchLucid.Application.Common;
using ArchLucid.Application.Decisions;
using ArchLucid.Application.Evidence;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Audit behavior on architecture run execute and commit orchestrators when runs are missing.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ArchitectureRunOrchestrationAuditTests
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
        Mock<IAuditService> durableAudit = new();

        ArchitectureRunExecuteOrchestrator sut = CreateExecuteOrchestrator(
            runRepo.Object,
            scopeProvider.Object,
            actor.Object,
            audit.Object,
            durableAudit.Object);

        Func<Task> act = () => sut.ExecuteRunAsync("missing");

        await act.Should().ThrowAsync<RunNotFoundException>();

        durableAudit.Verify(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);

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
        IArchitectureRunCommitOrchestrator commit = BuildCommitOrchestratorThatThrowsRunNotFound();

        Func<Task> act = () => commit.CommitRunAsync("missing");

        await act.Should().ThrowAsync<RunNotFoundException>();
    }

    private static ArchitectureRunExecuteOrchestrator CreateExecuteOrchestrator(
        IRunRepository runRepository,
        IScopeContextProvider scopeContextProvider,
        IActorContext actorContext,
        IBaselineMutationAuditService baselineMutationAudit,
        IAuditService auditService)
    {
        return new ArchitectureRunExecuteOrchestrator(
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
            auditService,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            new NoOpAgentOutputTraceEvaluationHook(),
            NullLogger<ArchitectureRunExecuteOrchestrator>.Instance);
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
