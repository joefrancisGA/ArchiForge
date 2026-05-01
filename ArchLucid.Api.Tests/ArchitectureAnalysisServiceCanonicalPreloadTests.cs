using ArchLucid.Application.Analysis;
using ArchLucid.Application.Determinism;
using ArchLucid.Application.Diagrams;
using ArchLucid.Application.Diffs;
using ArchLucid.Application.Summaries;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Decisioning.Interfaces;

using FluentAssertions;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Ensures analysis export uses the canonical <see cref="ArchitectureRunDetail" /> when the API
///     preloads it, avoiding redundant <see cref="IRunDetailQueryService" /> and manifest repository calls.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ArchitectureAnalysisServiceCanonicalPreloadTests
{
    private readonly Mock<IAgentResultDiffService> _agentResultDiffService = new();
    private readonly Mock<IDeterminismCheckService> _determinismCheckService = new();
    private readonly Mock<IDiagramGenerator> _diagramGenerator = new();
    private readonly Mock<IAgentEvidencePackageRepository> _evidenceRepository = new();
    private readonly Mock<IManifestDiffService> _manifestDiffService = new();
    private readonly Mock<IAgentResultRepository> _resultRepository = new();
    private readonly Mock<IRunDetailQueryService> _runDetailQueryService = new();
    private readonly Mock<IManifestSummaryGenerator> _summaryGenerator = new();
    private readonly ArchitectureAnalysisService _sut;
    private readonly Mock<IAgentExecutionTraceRepository> _traceRepository = new();
    private readonly Mock<IUnifiedGoldenManifestReader> _unifiedGoldenManifestReader = new();

    public ArchitectureAnalysisServiceCanonicalPreloadTests()
    {
        _evidenceRepository.Setup(r => r.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentEvidencePackage?)null);
        _traceRepository.Setup(r => r.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sut = new ArchitectureAnalysisService(
            _runDetailQueryService.Object,
            _unifiedGoldenManifestReader.Object,
            _evidenceRepository.Object,
            _traceRepository.Object,
            _resultRepository.Object,
            _diagramGenerator.Object,
            _summaryGenerator.Object,
            _determinismCheckService.Object,
            _manifestDiffService.Object,
            _agentResultDiffService.Object);
    }

    [SkippableFact]
    public async Task BuildAsync_WithPreloadedRunDetail_DoesNotRecallRunDetailOrPrimaryManifestFromRepository()
    {
        GoldenManifest manifest = new()
        {
            RunId = "run-1", SystemName = "Sys", Metadata = new ManifestMetadata { ManifestVersion = "v1" }
        };
        ArchitectureRunDetail detail = new()
        {
            Run = new ArchitectureRun
            {
                RunId = "run-1",
                RequestId = "req-1",
                Status = ArchitectureRunStatus.Committed,
                CurrentManifestVersion = "v1",
                CreatedUtc = DateTime.UtcNow
            },
            Manifest = manifest,
            Tasks = [],
            Results = []
        };

        ArchitectureAnalysisRequest request = new()
        {
            RunId = "run-1",
            PreloadedRunDetail = detail,
            IncludeEvidence = false,
            IncludeExecutionTraces = false,
            IncludeManifest = true,
            IncludeDiagram = false,
            IncludeSummary = false
        };

        ArchitectureAnalysisReport report = await _sut.BuildAsync(request);

        report.Manifest.Should().BeSameAs(manifest);
        _runDetailQueryService.Verify(
            s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()),
            Times.Never);
        _unifiedGoldenManifestReader.Verify(
            m => m.GetByVersionAsync("v1", It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task BuildAsync_WithPreloadedRunDetail_UsesManifestWhenCurrentManifestVersionEmpty()
    {
        GoldenManifest manifest = new()
        {
            RunId = "run-1", SystemName = "Sys", Metadata = new ManifestMetadata { ManifestVersion = "v1-run-1" }
        };
        ArchitectureRunDetail detail = new()
        {
            Run = new ArchitectureRun
            {
                RunId = "run-1",
                RequestId = "req-1",
                Status = ArchitectureRunStatus.TasksGenerated,
                CurrentManifestVersion = null,
                CreatedUtc = DateTime.UtcNow
            },
            Manifest = manifest,
            Tasks = [],
            Results = []
        };

        ArchitectureAnalysisRequest request = new()
        {
            RunId = "run-1",
            PreloadedRunDetail = detail,
            IncludeEvidence = false,
            IncludeExecutionTraces = false,
            IncludeManifest = true,
            IncludeDiagram = false,
            IncludeSummary = false
        };

        ArchitectureAnalysisReport report = await _sut.BuildAsync(request);

        report.Manifest.Should().BeSameAs(manifest);
        _unifiedGoldenManifestReader.Verify(
            m => m.GetByVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
