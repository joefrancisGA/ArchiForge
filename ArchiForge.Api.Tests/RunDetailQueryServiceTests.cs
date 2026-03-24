using ArchiForge.Application;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Unit tests for <see cref="RunDetailQueryService"/> — the canonical run detail assembly path.
/// </summary>
[Trait("Category", "Unit")]
public sealed class RunDetailQueryServiceTests
{
    private readonly Mock<IArchitectureRunRepository> _runRepo;
    private readonly Mock<IAgentTaskRepository> _taskRepo;
    private readonly Mock<IAgentResultRepository> _resultRepo;
    private readonly Mock<IGoldenManifestRepository> _manifestRepo;
    private readonly Mock<IDecisionTraceRepository> _traceRepo;
    private readonly RunDetailQueryService _sut;

    public RunDetailQueryServiceTests()
    {
        _runRepo = new Mock<IArchitectureRunRepository>();
        _taskRepo = new Mock<IAgentTaskRepository>();
        _resultRepo = new Mock<IAgentResultRepository>();
        _manifestRepo = new Mock<IGoldenManifestRepository>();
        _traceRepo = new Mock<IDecisionTraceRepository>();

        _sut = new RunDetailQueryService(
            _runRepo.Object,
            _taskRepo.Object,
            _resultRepo.Object,
            _manifestRepo.Object,
            _traceRepo.Object,
            new Mock<ILogger<RunDetailQueryService>>().Object);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ArchitectureRun CommittedRun(string runId = "run-1", string manifestVersion = "v1") => new()
    {
        RunId = runId,
        RequestId = "req-1",
        Status = ArchitectureRunStatus.Committed,
        CreatedUtc = DateTime.UtcNow,
        CompletedUtc = DateTime.UtcNow,
        CurrentManifestVersion = manifestVersion
    };

    private static ArchitectureRun InProgressRun(string runId = "run-2") => new()
    {
        RunId = runId,
        RequestId = "req-2",
        Status = ArchitectureRunStatus.ReadyForCommit,
        CreatedUtc = DateTime.UtcNow
    };

    private static GoldenManifest Manifest(string runId = "run-1", string version = "v1") => new()
    {
        RunId = runId,
        SystemName = "TestSystem",
        Metadata = new ManifestMetadata { ManifestVersion = version }
    };

    // ── GetRunDetailAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRunDetailAsync_RunNotFound_ReturnsNull()
    {
        _runRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRun?)null);

        var result = await _sut.GetRunDetailAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRunDetailAsync_CommittedRunWithManifest_ReturnsFullDetail()
    {
        var run = CommittedRun();
        var manifest = Manifest();
        var task = new AgentTask { TaskId = "t1", RunId = run.RunId };
        var agentResult = new AgentResult { ResultId = "r1", RunId = run.RunId };
        var trace = new DecisionTrace { TraceId = "tr1", RunId = run.RunId };

        _runRepo.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        _taskRepo.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentTask> { task });
        _resultRepo.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentResult> { agentResult });
        _manifestRepo.Setup(r => r.GetByVersionAsync("v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);
        _traceRepo.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DecisionTrace> { trace });

        var result = await _sut.GetRunDetailAsync("run-1");

        result.Should().NotBeNull();
        result!.Run.RunId.Should().Be("run-1");
        result.Tasks.Should().HaveCount(1);
        result.Results.Should().HaveCount(1);
        result.Manifest.Should().NotBeNull();
        result.Manifest!.RunId.Should().Be("run-1");
        result.DecisionTraces.Should().HaveCount(1);
        result.IsCommitted.Should().BeTrue();
    }

    [Fact]
    public async Task GetRunDetailAsync_RunNotYetCommitted_ReturnsDetailWithoutManifest()
    {
        var run = InProgressRun();

        _runRepo.Setup(r => r.GetByIdAsync("run-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        _taskRepo.Setup(r => r.GetByRunIdAsync("run-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentTask>());
        _resultRepo.Setup(r => r.GetByRunIdAsync("run-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentResult>());

        var result = await _sut.GetRunDetailAsync("run-2");

        result.Should().NotBeNull();
        result!.Manifest.Should().BeNull();
        result.DecisionTraces.Should().BeEmpty();
        result.IsCommitted.Should().BeFalse();

        // Manifest and trace repos must NOT be queried for uncommitted runs.
        _manifestRepo.Verify(r => r.GetByVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _traceRepo.Verify(r => r.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRunDetailAsync_ManifestMissing_ReturnsDetailWithNullManifest()
    {
        // Simulates storage corruption or replication lag: run says committed but manifest is gone.
        var run = CommittedRun();

        _runRepo.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        _taskRepo.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentTask>());
        _resultRepo.Setup(r => r.GetByRunIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentResult>());
        _manifestRepo.Setup(r => r.GetByVersionAsync("v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoldenManifest?)null);

        var result = await _sut.GetRunDetailAsync("run-1");

        result.Should().NotBeNull();
        result!.Run.RunId.Should().Be("run-1");
        result.Manifest.Should().BeNull();
        // Decision traces must NOT be queried when manifest is unavailable.
        _traceRepo.Verify(r => r.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRunDetailAsync_NullOrWhitespaceRunId_Throws()
    {
        Func<Task> act = () => _sut.GetRunDetailAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── ListRunSummariesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ListRunSummariesAsync_ReturnsMappedSummaries()
    {
        _runRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ArchitectureRunListItem>
            {
                new()
                {
                    RunId = "run-1",
                    RequestId = "req-1",
                    Status = "Committed",
                    CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CurrentManifestVersion = "v1",
                    SystemName = "Sys"
                },
                new()
                {
                    RunId = "run-2",
                    RequestId = "req-2",
                    Status = "ReadyForCommit",
                    CreatedUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    SystemName = "Sys2"
                }
            });

        var result = await _sut.ListRunSummariesAsync();

        result.Should().HaveCount(2);

        var first = result[0];
        first.RunId.Should().Be("run-1");
        first.Status.Should().Be("Committed");
        first.CurrentManifestVersion.Should().Be("v1");
        first.SystemName.Should().Be("Sys");

        var second = result[1];
        second.RunId.Should().Be("run-2");
        second.Status.Should().Be("ReadyForCommit");
        second.CurrentManifestVersion.Should().BeNull();
    }

    [Fact]
    public async Task ListRunSummariesAsync_EmptyRepository_ReturnsEmptyList()
    {
        _runRepo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ArchitectureRunListItem>());

        var result = await _sut.ListRunSummariesAsync();

        result.Should().BeEmpty();
    }
}
