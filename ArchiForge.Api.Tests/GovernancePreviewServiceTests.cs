using ArchiForge.Application;
using ArchiForge.Application.Governance.Preview;
using ArchiForge.Contracts.Architecture;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Governance;
using ArchiForge.Contracts.Governance.Preview;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

using FluentAssertions;

using Moq;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class GovernancePreviewServiceTests
{
    private readonly Mock<IGovernanceEnvironmentActivationRepository> _activationRepo = new();
    private readonly Mock<IRunDetailQueryService> _runDetailQueryService = new();
    private readonly Mock<IGoldenManifestRepository> _manifestRepo = new();
    private readonly GovernancePreviewService _sut;

    public GovernancePreviewServiceTests()
    {
        _sut = new GovernancePreviewService(
            _activationRepo.Object,
            _runDetailQueryService.Object,
            _manifestRepo.Object);
    }

    private static GoldenManifest Manifest(string runId, string version, Action<ManifestGovernance>? tweak = null)
    {
        ManifestGovernance gov = new ManifestGovernance();
        tweak?.Invoke(gov);
        return new GoldenManifest
        {
            RunId = runId,
            SystemName = "Sys",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = gov,
            Metadata = new ManifestMetadata { ManifestVersion = version, CreatedUtc = DateTime.UtcNow }
        };
    }

    private static ArchitectureRun Run(string runId) => new()
    {
        RunId = runId,
        RequestId = "req-1",
        Status = ArchitectureRunStatus.Committed,
        CreatedUtc = DateTime.UtcNow
    };

    /// <summary>
    /// Returns a run detail whose manifest is null (pre-commit), so the service falls back to
    /// <see cref="IGoldenManifestRepository.GetByVersionAsync"/> for the candidate manifest lookup.
    /// </summary>
    private static ArchitectureRunDetail RunDetail(string runId) => new()
    {
        Run = Run(runId),
        Manifest = null
    };

    [Fact]
    public async Task PreviewActivationAsync_WhenNoCurrentActiveRowExists_ReturnsPreviewAgainstEmptyCurrent()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RunDetail("run-a"));
        _manifestRepo.Setup(m => m.GetByVersionAsync("v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Manifest("run-a", "v1", g => g.RequiredControls.Add("PEP")));
        _activationRepo.Setup(a => a.GetByEnvironmentAsync("dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GovernanceEnvironmentActivation>());

        GovernancePreviewResult result = await _sut.PreviewActivationAsync(new GovernancePreviewRequest
        {
            RunId = "run-a",
            ManifestVersion = "v1",
            Environment = "dev"
        });

        result.CurrentRunId.Should().BeNull();
        result.CurrentManifestVersion.Should().BeNull();
        result.PreviewRunId.Should().Be("run-a");
        result.Differences.Should().Contain(d => d.Key == "RequiredControls" && d.ChangeType == GovernanceDiffChangeType.Added);
        result.Notes.Should().Contain(n => n.Contains("No current active governance activation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PreviewActivationAsync_WhenCurrentActiveRowExists_ReturnsDifferences()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RunDetail("run-b"));
        _manifestRepo.Setup(m => m.GetByVersionAsync("v2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Manifest("run-b", "v2", g =>
            {
                g.RequiredControls.Add("MI");
                g.RiskClassification = "High";
            }));

        GovernanceEnvironmentActivation currentActivation = new GovernanceEnvironmentActivation
        {
            ActivationId = "act-1",
            RunId = "run-old",
            ManifestVersion = "v-old",
            Environment = "test",
            IsActive = true
        };
        _activationRepo.Setup(a => a.GetByEnvironmentAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GovernanceEnvironmentActivation> { currentActivation });

        _manifestRepo.Setup(m => m.GetByVersionAsync("v-old", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Manifest("run-old", "v-old", g =>
            {
                g.RequiredControls.Add("PEP");
                g.RiskClassification = "Low";
            }));

        GovernancePreviewResult result = await _sut.PreviewActivationAsync(new GovernancePreviewRequest
        {
            RunId = "run-b",
            ManifestVersion = "v2",
            Environment = "test"
        });

        result.CurrentRunId.Should().Be("run-old");
        result.Differences.Should().Contain(d => d.Key == "RiskClassification" && d.ChangeType == GovernanceDiffChangeType.Changed);
        result.Differences.Should().Contain(d => d.Key == "RequiredControls" && d.ChangeType == GovernanceDiffChangeType.Changed);
    }

    [Fact]
    public async Task CompareEnvironmentsAsync_WhenBothHaveActiveRows_ReturnsDifferences()
    {
        GovernanceEnvironmentActivation srcAct = new GovernanceEnvironmentActivation
        {
            RunId = "r1",
            ManifestVersion = "m1",
            Environment = "dev",
            IsActive = true
        };
        GovernanceEnvironmentActivation tgtAct = new GovernanceEnvironmentActivation
        {
            RunId = "r2",
            ManifestVersion = "m2",
            Environment = "test",
            IsActive = true
        };

        _activationRepo.Setup(a => a.GetByEnvironmentAsync("dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync([srcAct]);
        _activationRepo.Setup(a => a.GetByEnvironmentAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync([tgtAct]);

        _manifestRepo.Setup(m => m.GetByVersionAsync("m1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Manifest("r1", "m1", g => g.CostClassification = "Low"));
        _manifestRepo.Setup(m => m.GetByVersionAsync("m2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Manifest("r2", "m2", g => g.CostClassification = "High"));

        GovernanceEnvironmentComparisonResult result = await _sut.CompareEnvironmentsAsync(new GovernanceEnvironmentComparisonRequest
        {
            SourceEnvironment = "dev",
            TargetEnvironment = "test"
        });

        result.SourceEnvironment.Should().Be("dev");
        result.TargetEnvironment.Should().Be("test");
        result.Differences.Should().Contain(d => d.Key == "CostClassification" && d.ChangeType == GovernanceDiffChangeType.Changed);
        result.Notes.Should().Contain(n => n.Contains("Compared active governance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CompareEnvironmentsAsync_WhenStatesAreEquivalent_ReturnsNoMeaningfulDiffs()
    {
        ManifestGovernance gov = new ManifestGovernance { RiskClassification = "Moderate", CostClassification = "Moderate" };
        GoldenManifest m = Manifest("r1", "v1", _ => { });
        m.Governance = gov;

        GovernanceEnvironmentActivation act1 = new GovernanceEnvironmentActivation { RunId = "r1", ManifestVersion = "v1", Environment = "dev", IsActive = true };
        GovernanceEnvironmentActivation act2 = new GovernanceEnvironmentActivation { RunId = "r2", ManifestVersion = "v2", Environment = "prod", IsActive = true };

        _activationRepo.Setup(a => a.GetByEnvironmentAsync("dev", It.IsAny<CancellationToken>())).ReturnsAsync([act1]);
        _activationRepo.Setup(a => a.GetByEnvironmentAsync("prod", It.IsAny<CancellationToken>())).ReturnsAsync([act2]);
        _manifestRepo.Setup(goldenManifestRepository => goldenManifestRepository.GetByVersionAsync("v1", It.IsAny<CancellationToken>())).ReturnsAsync(Manifest("r1", "v1"));
        _manifestRepo.Setup(goldenManifestRepository => goldenManifestRepository.GetByVersionAsync("v2", It.IsAny<CancellationToken>())).ReturnsAsync(Manifest("r2", "v2"));

        GovernanceEnvironmentComparisonResult result = await _sut.CompareEnvironmentsAsync(new GovernanceEnvironmentComparisonRequest
        {
            SourceEnvironment = "dev",
            TargetEnvironment = "prod"
        });

        result.Differences.Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewActivationAsync_DoesNotMutateActivationRows()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RunDetail("run-x"));
        _manifestRepo.Setup(m => m.GetByVersionAsync("v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Manifest("run-x", "v1"));
        _activationRepo.Setup(a => a.GetByEnvironmentAsync("dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.PreviewActivationAsync(new GovernancePreviewRequest
        {
            RunId = "run-x",
            ManifestVersion = "v1",
            Environment = "dev"
        });

        _activationRepo.Verify(a => a.CreateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()), Times.Never);
        _activationRepo.Verify(a => a.UpdateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreviewActivationAsync_WhenRunMissing_ThrowsRunNotFoundException()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        Func<Task<GovernancePreviewResult>> act = () => _sut.PreviewActivationAsync(new GovernancePreviewRequest
        {
            RunId = "missing",
            ManifestVersion = "v1",
            Environment = "dev"
        });

        await act.Should().ThrowAsync<RunNotFoundException>();
    }
}
