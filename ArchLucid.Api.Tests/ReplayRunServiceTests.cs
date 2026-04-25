using System.Data;

using ArchLucid.Application;
using ArchLucid.Application.Agents;
using ArchLucid.Application.Authority;
using ArchLucid.Application.Common;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Merge;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Ensures replay loads the source run through <see cref="IRunDetailQueryService" /> (canonical path)
///     rather than assembling run + tasks from separate repository calls.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReplayRunServiceTests
{
    private readonly Mock<IRunRepository> _authorityRunRepository = new();
    private readonly Mock<IDecisionEngineService> _decisionEngine = new();
    private readonly Mock<IAgentEvidencePackageRepository> _evidenceRepository = new();
    private readonly Mock<IAgentExecutorResolver> _executorResolver = new();
    private readonly Mock<IArchitectureRequestRepository> _requestRepository = new();

    private readonly Mock<IRunDetailQueryService> _runDetailQueryService = new();
    private readonly Mock<IScopeContextProvider> _scopeContextProvider = new();
    private readonly ReplayRunService _sut;

    public ReplayRunServiceTests()
    {
        _scopeContextProvider.Setup(p => p.GetCurrentScope()).Returns(new ScopeContext
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        });
        _authorityRunRepository
            .Setup(r => r.SaveAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null))
            .Returns(Task.CompletedTask);
        _authorityRunRepository.Setup(r =>
                r.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        // ADR 0030 PR A3 (2026-04-24): ICoordinatorDecisionTraceRepository was removed from
        // ReplayRunService — decision traces are persisted via IAuthorityCommittedManifestChainWriter only.
        _sut = new ReplayRunService(
            _executorResolver.Object,
            _decisionEngine.Object,
            _requestRepository.Object,
            _runDetailQueryService.Object,
            _authorityRunRepository.Object,
            _scopeContextProvider.Object,
            CreateAuthorityChainWriterMock().Object,
            _evidenceRepository.Object,
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            Mock.Of<IAuditService>(),
            UnitTestActor(),
            NullLogger<ReplayRunService>.Instance);
    }

    private static IActorContext UnitTestActor()
    {
        Mock<IActorContext> actor = new();
        actor.Setup(a => a.GetActor()).Returns("unit-test");

        return actor.Object;
    }

    private static Mock<IAuthorityCommittedManifestChainWriter> CreateAuthorityChainWriterMock()
    {
        Mock<IAuthorityCommittedManifestChainWriter> mock = new();
        mock.Setup(x => x.PersistCommittedChainAsync(
                It.IsAny<ScopeContext>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<GoldenManifest>(),
                It.IsAny<AuthorityChainKeying>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IDbConnection?>(),
                It.IsAny<IDbTransaction?>()))
            .ReturnsAsync((ScopeContext _, Guid _, string _, GoldenManifest _, AuthorityChainKeying k, DateTime _,
                    bool _, CancellationToken _, IDbConnection? _, IDbTransaction? _) =>
                new AuthorityManifestPersistResult(
                    k.ContextSnapshotId,
                    k.GraphSnapshotId,
                    k.FindingsSnapshotId,
                    k.DecisionTraceId,
                    k.ManifestId));

        return mock;
    }

    [Fact]
    public async Task ReplayAsync_WhenRunDetailMissing_ThrowsRunNotFoundException()
    {
        _runDetailQueryService
            .Setup(s => s.GetRunDetailAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        Func<Task<ReplayRunResult>> act = async () => await _sut.ReplayAsync("missing");

        await act.Should().ThrowAsync<RunNotFoundException>();
        _authorityRunRepository.Verify(
            r => r.SaveAsync(It.IsAny<RunRecord>(), It.IsAny<CancellationToken>(), null, null), Times.Never);
    }

    [Fact]
    public async Task ReplayAsync_WhenNoTasks_ThrowsInvalidOperationException()
    {
        _runDetailQueryService
            .Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchitectureRunDetail
            {
                Run = new ArchitectureRun
                {
                    RunId = "run-1", RequestId = "req-1", Status = ArchitectureRunStatus.Created
                },
                Tasks = [],
                Results = []
            });

        Func<Task<ReplayRunResult>> act = async () => await _sut.ReplayAsync("run-1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No tasks*");
    }
}
