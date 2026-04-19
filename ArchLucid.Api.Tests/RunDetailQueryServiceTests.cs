using ArchLucid.Application;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Unit tests for <see cref="RunDetailQueryService"/> — the canonical run detail assembly path.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RunDetailQueryServiceTests
{
    private readonly ScopeContext _scope = new()
    {
        TenantId = Guid.NewGuid(),
        WorkspaceId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid()
    };

    private readonly Guid _runGuid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _runGuid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly Mock<IRunRepository> _runRepo;
    private readonly Mock<IScopeContextProvider> _scopeProvider;
    private readonly Mock<IAgentTaskRepository> _taskRepo;
    private readonly Mock<IAgentResultRepository> _resultRepo;
    private readonly Mock<ICoordinatorGoldenManifestRepository> _manifestRepo;
    private readonly Mock<ICoordinatorDecisionTraceRepository> _traceRepo;
    private readonly RunDetailQueryService _sut;

    public RunDetailQueryServiceTests()
    {
        _runRepo = new Mock<IRunRepository>();
        _scopeProvider = new Mock<IScopeContextProvider>();
        _taskRepo = new Mock<IAgentTaskRepository>();
        _resultRepo = new Mock<IAgentResultRepository>();
        _manifestRepo = new Mock<ICoordinatorGoldenManifestRepository>();
        _traceRepo = new Mock<ICoordinatorDecisionTraceRepository>();

        _scopeProvider.Setup(s => s.GetCurrentScope()).Returns(_scope);

        _sut = new RunDetailQueryService(
            _runRepo.Object,
            _scopeProvider.Object,
            _taskRepo.Object,
            _resultRepo.Object,
            _manifestRepo.Object,
            _traceRepo.Object,
            new Mock<ILogger<RunDetailQueryService>>().Object);
    }

    private string Run1N => _runGuid1.ToString("N");

    private string Run2N => _runGuid2.ToString("N");

    private static GoldenManifest Manifest(string runId, string version = "v1") => new()
    {
        RunId = runId,
        SystemName = "TestSystem",
        Metadata = new ManifestMetadata { ManifestVersion = version }
    };

    private RunRecord CommittedRunRecord(string manifestVersion = "v1") => new()
    {
        RunId = _runGuid1,
        TenantId = _scope.TenantId,
        WorkspaceId = _scope.WorkspaceId,
        ScopeProjectId = _scope.ProjectId,
        ProjectId = "proj",
        ArchitectureRequestId = "req-1",
        LegacyRunStatus = ArchitectureRunStatus.Committed.ToString(),
        CreatedUtc = DateTime.UtcNow,
        CompletedUtc = DateTime.UtcNow,
        CurrentManifestVersion = manifestVersion
    };

    private RunRecord InProgressRunRecord() => new()
    {
        RunId = _runGuid2,
        TenantId = _scope.TenantId,
        WorkspaceId = _scope.WorkspaceId,
        ScopeProjectId = _scope.ProjectId,
        ProjectId = "proj",
        ArchitectureRequestId = "req-2",
        LegacyRunStatus = ArchitectureRunStatus.ReadyForCommit.ToString(),
        CreatedUtc = DateTime.UtcNow
    };

    // ── GetRunDetailAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRunDetailAsync_RunNotFound_ReturnsNull()
    {
        Guid missing = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        _runRepo.Setup(r => r.GetByIdAsync(_scope, missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RunRecord?)null);

        ArchitectureRunDetail? result = await _sut.GetRunDetailAsync(missing.ToString("N"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRunDetailAsync_CommittedRunWithManifest_ReturnsFullDetail()
    {
        RunRecord record = CommittedRunRecord();
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
        _manifestRepo.Setup(r => r.GetByVersionAsync("v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);
        _traceRepo.Setup(r => r.GetByRunIdAsync(Run1N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([trace]);

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

    [Fact]
    public async Task GetRunDetailAsync_RunNotYetCommitted_ReturnsDetailWithoutManifest()
    {
        RunRecord record = InProgressRunRecord();

        _runRepo.Setup(r => r.GetByIdAsync(_scope, _runGuid2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _taskRepo.Setup(r => r.GetByRunIdAsync(Run2N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _resultRepo.Setup(r => r.GetByRunIdAsync(Run2N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _manifestRepo.Setup(r => r.GetByVersionAsync($"v1-{Run2N}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldenManifest?)null);

        ArchitectureRunDetail? result = await _sut.GetRunDetailAsync(Run2N);

        result.Should().NotBeNull();
        result.Manifest.Should().BeNull();
        result.DecisionTraces.Should().BeEmpty();
        result.IsCommitted.Should().BeFalse();

        _manifestRepo.Verify(r => r.GetByVersionAsync($"v1-{Run2N}", It.IsAny<CancellationToken>()), Times.Once);
        _traceRepo.Verify(r => r.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRunDetailAsync_ManifestMissing_ReturnsDetailWithNullManifest()
    {
        RunRecord record = CommittedRunRecord();

        _runRepo.Setup(r => r.GetByIdAsync(_scope, _runGuid1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _taskRepo.Setup(r => r.GetByRunIdAsync(Run1N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _resultRepo.Setup(r => r.GetByRunIdAsync(Run1N, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _manifestRepo.Setup(r => r.GetByVersionAsync("v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldenManifest?)null);

        ArchitectureRunDetail? result = await _sut.GetRunDetailAsync(Run1N);

        result.Should().NotBeNull();
        result.Run.RunId.Should().Be(Run1N);
        result.Manifest.Should().BeNull();
        _traceRepo.Verify(r => r.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRunDetailAsync_NullOrWhitespaceRunId_Throws()
    {
        Func<Task> act = () => _sut.GetRunDetailAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetRunDetailAsync_InvalidRunId_ReturnsNull()
    {
        ArchitectureRunDetail? result = await _sut.GetRunDetailAsync("not-a-guid");

        result.Should().BeNull();
    }

    // ── ListRunSummariesAsync ─────────────────────────────────────────────────

    [Fact]
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
                    LegacyRunStatus = ArchitectureRunStatus.Committed.ToString(),
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
                    LegacyRunStatus = ArchitectureRunStatus.ReadyForCommit.ToString(),
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

    [Fact]
    public async Task ListRunSummariesAsync_EmptyRepository_ReturnsEmptyList()
    {
        _runRepo.Setup(r => r.ListRecentInScopeAsync(_scope, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        IReadOnlyList<RunSummary> result = await _sut.ListRunSummariesAsync();

        result.Should().BeEmpty();
    }
}
