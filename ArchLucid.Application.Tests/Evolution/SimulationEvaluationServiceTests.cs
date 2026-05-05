using ArchLucid.Application.Analysis;
using ArchLucid.Application.Determinism;
using ArchLucid.Application.Diffs;
using ArchLucid.Application.Evolution;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Evolution;

[Trait("Category", "Unit")]
public sealed class SimulationEvaluationServiceTests
{
    [SkippableFact]
    public async Task EvaluateAsync_twice_same_inputs_yields_identical_scores()
    {
        SimulationEvaluationService sut = CreateSut();

        SimulationEvaluationRequest request = BuildRequest(warningsBaseline: 2, warningsSimulated: 1);

        SimulationEvaluationResult a = await sut.EvaluateAsync(request, CancellationToken.None);
        SimulationEvaluationResult b = await sut.EvaluateAsync(request, CancellationToken.None);

        a.Score.ImprovementDelta.Should().Be(b.Score.ImprovementDelta);
        a.Score.SimulationScore.Should().Be(b.Score.SimulationScore);
        a.Score.ConfidenceScore.Should().Be(b.Score.ConfidenceScore);
        a.ExplanationSummary.Should().Be(b.ExplanationSummary);
    }

    [SkippableFact]
    public async Task EvaluateAsync_supplied_determinism_non_deterministic_sets_score_zero_and_signal()
    {
        Mock<IManifestDiffService> manifestDiff = new();
        Mock<IDeterminismCheckService> determinism = new();

        SimulationEvaluationService sut = new(manifestDiff.Object, determinism.Object);

        DeterminismCheckResult det = new() { SourceRunId = "r1", Iterations = 2, IsDeterministic = false, };

        SimulationEvaluationRequest request = new()
        {
            BaselineReport = new ArchitectureAnalysisReport { Run = new ArchitectureRun { RunId = "r1" }, Warnings = [], },
            SimulatedReport = new ArchitectureAnalysisReport { Run = new ArchitectureRun { RunId = "r1" }, Warnings = [], },
            SuppliedDeterminism = det,
        };

        SimulationEvaluationResult result = await sut.EvaluateAsync(request, CancellationToken.None);

        determinism.Verify(
            x => x.RunAsync(It.IsAny<DeterminismCheckRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        result.Score.DeterminismScore.Should().Be(0);
        result.Score.RegressionSignals.Should().Contain("Determinism.ReplayDrift");
    }

    [SkippableFact]
    public async Task EvaluateAsync_computes_manifest_diff_when_not_preloaded()
    {
        GoldenManifest left = MinimalManifest("r1", "v1");
        GoldenManifest right = MinimalManifest("r1", "v1");

        Mock<IManifestDiffService> manifestDiff = new();
        manifestDiff
            .Setup(x => x.Compare(left, right))
            .Returns(
                new ManifestDiffResult { LeftManifestVersion = "v1", RightManifestVersion = "v1", RemovedServices = ["S1"], });

        SimulationEvaluationService sut = new(manifestDiff.Object, Mock.Of<IDeterminismCheckService>());

        SimulationEvaluationRequest request = new()
        {
            BaselineReport =
                new ArchitectureAnalysisReport
                {
                    Run = new ArchitectureRun { RunId = "r1", CurrentManifestVersion = "v1" }, Manifest = left, Warnings = [],
                },
            SimulatedReport = new ArchitectureAnalysisReport
            {
                Run = new ArchitectureRun { RunId = "r1", CurrentManifestVersion = "v1" }, Manifest = right, Warnings = [],
            },
        };

        SimulationEvaluationResult result = await sut.EvaluateAsync(request, CancellationToken.None);

        manifestDiff.Verify(x => x.Compare(left, right), Times.Once);
        result.Score.RegressionSignals.Should().Contain(s => s.StartsWith("Regression.RemovedServices:", StringComparison.Ordinal));
        result.Score.RegressionRiskScore.Should().BeGreaterThan(0);
    }

    private static SimulationEvaluationService CreateSut()
    {
        Mock<IManifestDiffService> manifestDiff = new();
        manifestDiff
            .Setup(x => x.Compare(It.IsAny<GoldenManifest>(), It.IsAny<GoldenManifest>()))
            .Returns(
                new ManifestDiffResult { LeftManifestVersion = "v1", RightManifestVersion = "v1", });

        return new SimulationEvaluationService(manifestDiff.Object, Mock.Of<IDeterminismCheckService>());
    }

    private static SimulationEvaluationRequest BuildRequest(int warningsBaseline, int warningsSimulated)
    {
        List<string> bw = Enumerable.Repeat("w", warningsBaseline).ToList();
        List<string> sw = Enumerable.Repeat("x", warningsSimulated).ToList();

        GoldenManifest m = MinimalManifest("r1", "v1");

        return new SimulationEvaluationRequest
        {
            BaselineReport =
                new ArchitectureAnalysisReport { Run = new ArchitectureRun { RunId = "r1", CurrentManifestVersion = "v1" }, Manifest = m, Warnings = bw, },
            SimulatedReport = new ArchitectureAnalysisReport
            {
                Run = new ArchitectureRun { RunId = "r1", CurrentManifestVersion = "v1" }, Manifest = m, Warnings = sw,
            },
        };
    }

    private static GoldenManifest MinimalManifest(string runId, string version)
    {
        return new GoldenManifest { RunId = runId, SystemName = "Sys", Metadata = new ManifestMetadata { ManifestVersion = version }, };
    }
}
