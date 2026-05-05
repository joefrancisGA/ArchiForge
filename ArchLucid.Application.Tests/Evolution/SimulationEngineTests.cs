using ArchLucid.Application.Analysis;
using ArchLucid.Application.Diffs;
using ArchLucid.Application.Evolution;
using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.Metadata;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Evolution;

[Trait("Category", "Unit")]
public sealed class SimulationEngineTests
{
    [SkippableFact]
    public async Task SimulateAsync_SinglePass_CallsBuildAsyncOnce_AndNeverEnablesDeterminism()
    {
        Mock<IArchitectureAnalysisService> analysis = new();
        ArchitectureAnalysisReport report = CreateReport("run-a", "v1", "summary", ["a"]);

        ArchitectureAnalysisRequest? captured = null;

        analysis
            .Setup(x => x.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ArchitectureAnalysisRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(report);

        SimulationEngine sut = new(analysis.Object);

        SimulationRequest request = new() { CandidateChangeSet = MinimalCandidate(), BaselineArchitectureRunId = "run-a", };

        SimulationResult result = await sut.SimulateAsync(request, CancellationToken.None);

        analysis.Verify(
            x => x.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);

        captured.Should().NotBeNull();
        captured!.IncludeDeterminismCheck.Should().BeFalse();
        result.Warnings.Should().Equal("a");
        result.Artifacts.Should().NotBeNull();
        result.Artifacts!.BaselineSummaryPreview.Should().Be("summary");
        result.Diff!.Summary.Should().Contain("Single read-only pass");
    }

    [SkippableFact]
    public async Task SimulateAsync_TwoProfiles_CallsBuildAsyncTwice_AndPrefixesWarnings()
    {
        Mock<IArchitectureAnalysisService> analysis = new();

        ArchitectureAnalysisReport baseline = CreateReport("run-b", "v1", "left", ["w1"]);
        ArchitectureAnalysisReport simulated = CreateReport("run-b", "v1", "right", ["w2"]);
        simulated.ManifestDiff = new ManifestDiffResult { LeftManifestVersion = "v0", RightManifestVersion = "v1", AddedServices = ["S"], };

        analysis
            .SetupSequence(x => x.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline)
            .ReturnsAsync(simulated);

        SimulationReadProfile extended = new()
        {
            IncludeManifest = true, IncludeSummary = true, IncludeManifestCompare = true, CompareManifestVersion = "v0",
        };

        SimulationEngine sut = new(analysis.Object);

        SimulationRequest request = new()
        {
            CandidateChangeSet = MinimalCandidate(),
            BaselineArchitectureRunId = "run-b",
            Options = new SimulationEngineOptions { SimulatedReadProfile = extended },
        };

        SimulationResult result = await sut.SimulateAsync(request, CancellationToken.None);

        analysis.Verify(
            x => x.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        result.Warnings.Should().Contain("Baseline: w1");
        result.Warnings.Should().Contain("Simulated: w2");
        result.Diff!.DetailJson.Should().NotBeNullOrWhiteSpace();
        result.Scores!.RegressionRiskScore.Should().Be(0);
    }

    [SkippableFact]
    public async Task SimulateAsync_NullCandidate_Throws()
    {
        SimulationEngine sut = new(Mock.Of<IArchitectureAnalysisService>());

        SimulationRequest request = new() { CandidateChangeSet = null!, BaselineArchitectureRunId = "x", };

        Func<Task> act = async () => await sut.SimulateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static CandidateChangeSet MinimalCandidate()
    {
        return new CandidateChangeSet
        {
            ChangeSetId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            SourcePlanId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Description = "test",
            CreatedUtc = DateTime.UtcNow,
            ApprovalStatus = ApprovalStatus.PendingReview,
        };
    }

    private static ArchitectureAnalysisReport CreateReport(
        string runId,
        string manifestVersion,
        string summary,
        IReadOnlyList<string> warnings)
    {
        return new ArchitectureAnalysisReport
        {
            Run = new ArchitectureRun { RunId = runId, CurrentManifestVersion = manifestVersion }, Summary = summary, Warnings = [.. warnings],
        };
    }
}
