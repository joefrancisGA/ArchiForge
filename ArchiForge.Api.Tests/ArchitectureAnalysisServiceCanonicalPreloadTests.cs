using ArchiForge.Application;
using ArchiForge.Application.Analysis;
using ArchiForge.Application.Determinism;
using ArchiForge.Application.Diagrams;
using ArchiForge.Application.Diffs;
using ArchiForge.Application.Summaries;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

using FluentAssertions;

using Moq;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Ensures analysis export uses the canonical <see cref="ArchitectureRunDetail"/> when the API
/// preloads it, avoiding redundant <see cref="IRunDetailQueryService"/> and manifest repository calls.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ArchitectureAnalysisServiceCanonicalPreloadTests
{
    private readonly Mock<IRunDetailQueryService> _runDetailQueryService = new();
    private readonly Mock<IGoldenManifestRepository> _manifestRepository = new();
    private readonly Mock<IAgentEvidencePackageRepository> _evidenceRepository = new();
    private readonly Mock<IAgentExecutionTraceRepository> _traceRepository = new();
    private readonly Mock<IAgentResultRepository> _resultRepository = new();
    private readonly Mock<IDiagramGenerator> _diagramGenerator = new();
    private readonly Mock<IManifestSummaryGenerator> _summaryGenerator = new();
    private readonly Mock<IDeterminismCheckService> _determinismCheckService = new();
    private readonly Mock<IManifestDiffService> _manifestDiffService = new();
    private readonly Mock<IAgentResultDiffService> _agentResultDiffService = new();
    private readonly ArchitectureAnalysisService _sut;

    public ArchitectureAnalysisServiceCanonicalPreloadTests()
    {
        _evidenceRepository.Setup(r => r.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentEvidencePackage?)null);
        _traceRepository.Setup(r => r.GetByRunIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sut = new ArchitectureAnalysisService(
            _runDetailQueryService.Object,
            _manifestRepository.Object,
            _evidenceRepository.Object,
            _traceRepository.Object,
            _resultRepository.Object,
            _diagramGenerator.Object,
            _summaryGenerator.Object,
            _determinismCheckService.Object,
            _manifestDiffService.Object,
            _agentResultDiffService.Object);
    }

    [Fact]
    public async Task BuildAsync_WithPreloadedRunDetail_DoesNotRecallRunDetailOrPrimaryManifestFromRepository()
    {
        GoldenManifest manifest = new GoldenManifest
        {
            RunId = "run-1",
            SystemName = "Sys",
            Metadata = new ManifestMetadata { ManifestVersion = "v1" }
        };
        ArchitectureRunDetail detail = new ArchitectureRunDetail
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

        ArchitectureAnalysisRequest request = new ArchitectureAnalysisRequest
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
        _manifestRepository.Verify(
            m => m.GetByVersionAsync("v1", It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
