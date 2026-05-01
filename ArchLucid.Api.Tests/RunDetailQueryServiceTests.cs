using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="RunDetailQueryService" /> â€” the canonical run detail assembly path.
/// </summary>
/// <remarks>
///     ADR 0030 PR A3 (2026-04-24): the legacy <c>ICoordinatorDecisionTraceRepository</c> was deleted.
///     Decision traces now come from the authority repository keyed by <see cref="RunRecord.DecisionTraceId" />.
/// </remarks>
[Trait("Category", "Unit")]
public sealed class RunDetailQueryServiceTests
{
    private readonly Mock<IDecisionTraceRepository> _authorityTraceRepo;
    private readonly Mock<IAgentResultRepository> _resultRepo;

    private readonly Guid _runGuid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _runGuid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly Mock<IRunRepository> _runRepo;

    private readonly ScopeContext _scope = new()
    {
        TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
    };

    private readonly RunDetailQueryService _sut;
    private readonly Mock<IAgentTaskRepository> _taskRepo;
    private readonly Mock<IUnifiedGoldenManifestReader> _unifiedManifestReader;

    public RunDetailQueryServiceTests()
    {
        _runRepo = new Mock<IRunRepository>();
        Mock<IScopeContextProvider> scopeProvider = new();
        _taskRepo = new Mock<IAgentTaskRepository>();
        _resultRepo = new Mock<IAgentResultRepository>();
        _unifiedManifestReader = new Mock<IUnifiedGoldenManifestReader>();
        _authorityTraceRepo = new Mock<IDecisionTraceRepository>();

        scopeProvider.Setup(s => s.GetCurrentScope()).Returns(_scope);

        _sut = new RunDetailQueryService(
            _runRepo.Object,
            scopeProvider.Object,
            _taskRepo.Object,
            _resultRepo.Object,
            _unifiedManifestReader.Object,
            _authorityTraceRepo.Object,
            new Mock<ILogger<RunDetailQueryService>>().Object);
    }

    private string Run1N => _runGuid1.ToString("N");

    private string Run2N => _runGuid2.ToString("N");

    private static GoldenManifest Manifest(string runId, string version = "v1")
    {
        return new GoldenManifest
        {
            RunId = runId, SystemName = "TestSystem", Metadata = new ManifestMetadata { ManifestVersion = version }
        };
    }

    private RunRecord CommittedRunRecord(string manifestVersion = "v1", Guid? decisionTraceId = null)
    {
        return new RunRecord
        {
            RunId = _runGuid1,
            TenantId = _scope.TenantId,
            WorkspaceId = _scope.WorkspaceId,
            ScopeProjectId = _scope.ProjectId,
            ProjectId = "proj",
            ArchitectureRequestId = "req-1",
            LegacyRunStatus = nameof(ArchitectureRunStatus.Committed),
            CreatedUtc = DateTime.UtcNow,
            CompletedUtc = DateTime.UtcNow,
            CurrentManifestVersion = manifestVersion,
            DecisionTraceId = decisionTraceId
        };
    }

    private RunRecord InProgressRunRecord()
    {
        return new RunRecord
        {
            RunId = _runGuid2,
            TenantId = _scope.TenantId,
            WorkspaceId = _scope.WorkspaceId,
            ScopeProjectId = _scope.ProjectId,
            ProjectId = "proj",
            ArchitectureRequestId = "req-2",
            LegacyRunStatus = nameof(ArchitectureRunStatus.ReadyForCommit),
            CreatedUtc = DateTime.UtcNow
        };
    }

    [SkippableFact]
    public async Task GetRunDetailAsync_RunNotFound_ReturnsNull()
    {
        Guid missing = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        _runRepo.Setup(r => r.GetByIdAsync(_scope, missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        ArchitectureRunDetail? result = await _sut.GetRunDetailAsync(missing.ToString("N"));

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetRunDetailAsync_CommittedRunWithManifest_ReturnsFullDetail()
    {
        Guid traceId = Guid.NewGuid();
        RunRecord record = CommittedRunRecord(decisionTraceId: traceId);
        GoldenManifest manifest = Manifest(Run1N);
        AgentTask task = new() { TaskId = "t1", RunId = Run1N };
        AgentResult agentResult = new() { ResultId = "r1", RunId = Run1N };
        DecisionTrace trace = RunEventTrace.From(new RunEventTracePayload { TraceId = "tr1", RunId = Run1N });

        _runRepo.Setup(r => r.GetByIdAsync(_scope, _runGuid1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _taskRepo.Setup(r => r.GetByRunIdAsync(Run1N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([task]);
        _resultRepo.Setup(r => r.GetByRunIdAsync(Run1N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([agentResult]);
        _unifiedManifestReader
            .Setup(r => r.ReadByRunIdAsync(_scope, _runGuid1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);
        _authorityTraceRepo.Setup(r => r.GetByIdAsync(_scope, traceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trace);

        ArchitectureRunDetail? result = await _sut.GetRunDetailAsync(Run1N);

        result.Should().NotBeNull();
        result.Run.RunId.Should().Be(Run1N);
        result.Tasks.Should().HaveCount(1);
        result.Results.Should().HaveCount(1);
        result.Manifest.Should().NotBeNull();
        result.Manifest!.RunId.Should().Be(Run1N);
        result.DecisionTraces.Should().HaveCount(1);
        result.IsCommitted.Should().BeTrue();
    }

    [SkippableFact]
    public async Task GetRunDetailAsync_CommittedRunWithoutDecisionTraceId_ReturnsEmptyTraces()
    {
        RunRecord record = CommittedRunRecord(decisionTraceId: null);
        GoldenManifest manifest = Manifest(Run1N);

        _runRepo.Setup(r => r.GetByIdAsync(_scope, _runGuid1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _taskRepo.Setup(r => r.GetByRunIdAsync(Run1N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _resultRepo.Setup(r => r.GetByRunIdAsync(Run1N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _unifiedManifestReader
            .Setup(r => r.ReadByRunIdAsync(_scope, _runGuid1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        ArchitectureRunDetail? result = await _sut.GetRunDetailAsync(Run1N);

        result.Should().NotBeNull();
        result.DecisionTraces.Should().BeEmpty();
        _authorityTraceRepo.Verify(
            r => r.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task GetRunDetailAsync_RunNotYetCommitted_ReturnsDetailWithoutManifest()
    {
        RunRecord record = InProgressRunRecord();

        _runRepo.Setup(r => r.GetByIdAsync(_scope, _runGuid2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _taskRepo.Setup(r => r.GetByRunIdAsync(Run2N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _resultRepo.Setup(r => r.GetByRunIdAsync(Run2N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _unifiedManifestReader
            .Setup(r => r.ReadByRunIdAsync(_scope, _runGuid2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldenManifest?)null);

        ArchitectureRunDetail? result = await _sut.GetRunDetailAsync(Run2N);

        result.Should().NotBeNull();
        result.Manifest.Should().BeNull();
        result.DecisionTraces.Should().BeEmpty();
        result.IsCommitted.Should().BeFalse();

        _unifiedManifestReader.Verify(r => r.ReadByRunIdAsync(_scope, _runGuid2, It.IsAny<CancellationToken>()),
            Times.Once);
        _authorityTraceRepo.Verify(
            r => r.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task GetRunDetailAsync_ManifestMissing_ReturnsDetailWithNullManifest()
    {
        RunRecord record = CommittedRunRecord();

        _runRepo.Setup(r => r.GetByIdAsync(_scope, _runGuid1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _taskRepo.Setup(r => r.GetByRunIdAsync(Run1N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _resultRepo.Setup(r => r.GetByRunIdAsync(Run1N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _unifiedManifestReader
            .Setup(r => r.ReadByRunIdAsync(_scope, _runGuid1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldenManifest?)null);

        ArchitectureRunDetail? result = await _sut.GetRunDetailAsync(Run1N);

        result.Should().NotBeNull();
        result.Run.RunId.Should().Be(Run1N);
        result.Manifest.Should().BeNull();
        _authorityTraceRepo.Verify(
            r => r.GetByIdAsync(It.IsAny<ScopeContext>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task GetRunDetailAsync_NullOrWhitespaceRunId_Throws()
    {
        Func<Task> act = () => _sut.GetRunDetailAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [SkippableFact]
    public async Task GetRunDetailAsync_InvalidRunId_ReturnsNull()
    {
        ArchitectureRunDetail? result = await _sut.GetRunDetailAsync("not-a-guid");

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task ListRunSummariesAsync_ReturnsMappedSummaries()
    {
        Guid g1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid g2 = Guid.Parse("44444444-4444-4444-4444-444444444444");

        _runRepo.Setup(r => r.ListRecentInScopeAsync(_scope, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new RunRecord
                {
                    RunId = g1,
                    TenantId = _scope.TenantId,
                    WorkspaceId = _scope.WorkspaceId,
                    ScopeProjectId = _scope.ProjectId,
                    ProjectId = "Sys",
                    ArchitectureRequestId = "req-1",
                    LegacyRunStatus = nameof(ArchitectureRunStatus.Committed),
                    CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CurrentManifestVersion = "v1"
                },
                new RunRecord
                {
                    RunId = g2,
                    TenantId = _scope.TenantId,
                    WorkspaceId = _scope.WorkspaceId,
                    ScopeProjectId = _scope.ProjectId,
                    ProjectId = "Sys2",
                    ArchitectureRequestId = "req-2",
                    LegacyRunStatus = nameof(ArchitectureRunStatus.ReadyForCommit),
                    CreatedUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
                }
            ]);

        IReadOnlyList<RunSummary> result = await _sut.ListRunSummariesAsync();

        result.Should().HaveCount(2);

        RunSummary first = result[0];
        first.RunId.Should().Be(g1.ToString("N"));
        first.Status.Should().Be("Committed");
        first.CurrentManifestVersion.Should().Be("v1");
        first.SystemName.Should().Be("Sys");

        RunSummary second = result[1];
        second.RunId.Should().Be(g2.ToString("N"));
        second.Status.Should().Be("ReadyForCommit");
        second.CurrentManifestVersion.Should().BeNull();
    }

    [SkippableFact]
    public async Task ListRunSummariesAsync_EmptyRepository_ReturnsEmptyList()
    {
        _runRepo.Setup(r => r.ListRecentInScopeAsync(_scope, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        IReadOnlyList<RunSummary> result = await _sut.ListRunSummariesAsync();

        result.Should().BeEmpty();
    }
}
