using ArchLucid.Application.Common;
using ArchLucid.Application.Runs.Finalization;
using ArchLucid.Application.Runs.Orchestration;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.KnowledgeGraph.Interfaces;
using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Runs.Orchestration;

/// <summary>
/// Guards and error mapping at the entry of <see cref="AuthorityDrivenArchitectureRunCommitOrchestrator.CommitRunAsync"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AuthorityDrivenArchitectureRunCommitOrchestratorCommitRunAsyncGuardTests
{
    [SkippableFact]
    public async Task CommitRunAsync_null_runId_throws_ArgumentNullException()
    {
        AuthorityDrivenArchitectureRunCommitOrchestrator sut = CreateSut(
            out _,
            out _,
            out _);

        string runIdNull = null!;

        Func<Task> act = async () => await sut.CommitRunAsync(runIdNull, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [SkippableFact]
    public async Task CommitRunAsync_whitespace_runId_throws_ArgumentException()
    {
        AuthorityDrivenArchitectureRunCommitOrchestrator sut = CreateSut(
            out _,
            out _,
            out _);

        Func<Task> act = async () => await sut.CommitRunAsync("   ", CancellationToken.None);

        (await act.Should().ThrowAsync<ArgumentException>())
            .Which.ParamName.Should().Be("runId");
    }

    [SkippableFact]
    public async Task CommitRunAsync_malformed_run_id_throws_RunNotFoundException_and_records_baseline_audit()
    {
        AuthorityDrivenArchitectureRunCommitOrchestrator sut = CreateSut(
            out Mock<IBaselineMutationAuditService> baselineAudit,
            out _,
            out _);

        Func<Task> act = async () => await sut.CommitRunAsync("not-a-guid", CancellationToken.None);

        await act.Should().ThrowAsync<RunNotFoundException>();
        baselineAudit.Verify(
            b => b.RecordAsync(
                AuditEventTypes.Baseline.Architecture.RunFailed,
                It.IsAny<string>(),
                "not-a-guid",
                "Run not found.",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [SkippableFact]
    public async Task CommitRunAsync_known_guid_missing_from_repository_throws_RunNotFoundException()
    {
        Guid runGuid = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        string runId = runGuid.ToString("N");
        AuthorityDrivenArchitectureRunCommitOrchestrator sut = CreateSut(
            out Mock<IBaselineMutationAuditService> baselineAudit,
            out Mock<IRunRepository> runRepository,
            out Mock<IScopeContextProvider> scopeProvider);

        scopeProvider.Setup(p => p.GetCurrentScope()).Returns(new ScopeContext
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        });
        runRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<ScopeContext>(), runGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        Func<Task> act = async () => await sut.CommitRunAsync(runId, CancellationToken.None);

        await act.Should().ThrowAsync<RunNotFoundException>();
        baselineAudit.Verify(
            b => b.RecordAsync(
                AuditEventTypes.Baseline.Architecture.RunFailed,
                It.IsAny<string>(),
                runId,
                "Run not found.",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AuthorityDrivenArchitectureRunCommitOrchestrator CreateSut(
        out Mock<IBaselineMutationAuditService> baselineAudit,
        out Mock<IRunRepository> runRepository,
        out Mock<IScopeContextProvider> scopeProvider)
    {
        baselineAudit = new Mock<IBaselineMutationAuditService>();
        baselineAudit
            .Setup(b => b.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        runRepository = new Mock<IRunRepository>();
        scopeProvider = new Mock<IScopeContextProvider>();

        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("unit-test-actor");

        return new AuthorityDrivenArchitectureRunCommitOrchestrator(
            runRepository.Object,
            scopeProvider.Object,
            Mock.Of<IAgentTaskRepository>(),
            Mock.Of<IArchitectureRequestRepository>(),
            Mock.Of<IAgentEvidencePackageRepository>(),
            Mock.Of<IAgentResultRepository>(),
            Mock.Of<IGraphSnapshotRepository>(),
            Mock.Of<IFindingsSnapshotRepository>(),
            Mock.Of<IDecisionEngine>(),
            Mock.Of<IDecisionTraceRepository>(),
            Mock.Of<IGoldenManifestRepository>(),
            Mock.Of<IAuthorityCommitProjectionBuilder>(),
            Mock.Of<IManifestFinalizationService>(),
            Mock.Of<IPreCommitGovernanceGate>(),
            Options.Create(new PreCommitGovernanceGateOptions()),
            actor.Object,
            baselineAudit.Object,
            Mock.Of<IAuditService>(),
            Mock.Of<ITrialFunnelCommitHook>(),
            Mock.Of<IFirstSessionLifecycleHook>(),
            Mock.Of<IDbConnectionFactory>(),
            Mock.Of<ILogger<AuthorityDrivenArchitectureRunCommitOrchestrator>>());
    }
}
