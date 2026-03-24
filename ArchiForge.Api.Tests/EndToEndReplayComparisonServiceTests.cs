using ArchiForge.Application;
using ArchiForge.Application.Analysis;
using ArchiForge.Application.Diffs;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

using FluentAssertions;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Ensures end-to-end run comparison loads both runs through <see cref="IRunDetailQueryService"/>
/// (49R canonical path) rather than ad hoc repository assembly.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EndToEndReplayComparisonServiceTests
{
    private readonly Mock<IRunDetailQueryService> _runDetailQueryService = new();
    private readonly Mock<IRunExportRecordRepository> _exportRepo = new();
    private readonly Mock<IAgentResultDiffService> _agentDiff = new();
    private readonly Mock<IManifestDiffService> _manifestDiff = new();
    private readonly Mock<IExportRecordDiffService> _exportDiff = new();
    private readonly EndToEndReplayComparisonService _sut;

    public EndToEndReplayComparisonServiceTests()
    {
        _sut = new EndToEndReplayComparisonService(
            _runDetailQueryService.Object,
            _exportRepo.Object,
            _agentDiff.Object,
            _manifestDiff.Object,
            _exportDiff.Object);
    }

    private static ArchitectureRun Run(string id, string? manifestVersion = null) => new()
    {
        RunId = id,
        RequestId = "req",
        Status = ArchitectureRunStatus.Committed,
        CreatedUtc = DateTime.UtcNow,
        CurrentManifestVersion = manifestVersion
    };

    private static GoldenManifest Manifest(string runId, string version) => new()
    {
        RunId = runId,
        SystemName = "Sys",
        Metadata = new ManifestMetadata { ManifestVersion = version }
    };

    [Fact]
    public async Task BuildAsync_LoadsBothRunsViaRunDetailQueryService_AndComparesManifestsFromDetail()
    {
        var left = new ArchitectureRunDetail
        {
            Run = Run("left", "vL"),
            Results = [new AgentResult { RunId = "left", TaskId = "t1", AgentType = AgentType.Topology }],
            Manifest = Manifest("left", "vL")
        };
        var right = new ArchitectureRunDetail
        {
            Run = Run("right", "vR"),
            Results = [],
            Manifest = Manifest("right", "vR")
        };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("left", It.IsAny<CancellationToken>())).ReturnsAsync(left);
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("right", It.IsAny<CancellationToken>())).ReturnsAsync(right);
        _exportRepo.Setup(r => r.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RunExportRecord>());
        _manifestDiff.Setup(m => m.Compare(left.Manifest!, right.Manifest!)).Returns(new ManifestDiffResult());
        _agentDiff.Setup(a => a.Compare("left", left.Results, "right", right.Results))
            .Returns(new AgentResultDiffResult());

        var report = await _sut.BuildAsync("left", "right");

        report.LeftRunId.Should().Be("left");
        report.RightRunId.Should().Be("right");
        _runDetailQueryService.Verify(s => s.GetRunDetailAsync("left", It.IsAny<CancellationToken>()), Times.Once);
        _runDetailQueryService.Verify(s => s.GetRunDetailAsync("right", It.IsAny<CancellationToken>()), Times.Once);
        _manifestDiff.Verify(m => m.Compare(left.Manifest!, right.Manifest!), Times.Once);
    }

    [Fact]
    public async Task BuildAsync_WhenLeftRunMissing_ThrowsRunNotFoundException()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        var act = () => _sut.BuildAsync("missing", "right");

        await act.Should().ThrowAsync<RunNotFoundException>().WithMessage("*missing*");
    }
}
