using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Analysis;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Models;

using FluentAssertions;

using Moq;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// <see cref="ImprovementAdvisorService"/> wires learning profile load → signal analysis → recommendation generation → summary mapping.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ImprovementAdvisorServiceTests
{
    [Fact]
    public async Task GeneratePlanAsync_WithoutComparison_CallsAnalyzerWithNullComparison_AndMapsRunId()
    {
        GoldenManifest manifest = CreateManifest();
        FindingsSnapshot findings = CreateFindings(manifest.RunId);

        Mock<IImprovementSignalAnalyzer> analyzer = new();
        analyzer
            .Setup(a => a.Analyze(manifest, findings, null))
            .Returns([]);

        Mock<IRecommendationGenerator> generator = new();
        generator
            .Setup(g => g.Generate(It.IsAny<IReadOnlyList<ImprovementSignal>>(), It.IsAny<RecommendationLearningProfile?>()))
            .Returns([]);

        Mock<IRecommendationLearningService> learning = new();
        learning
            .Setup(s => s.GetLatestProfileAsync(manifest.TenantId, manifest.WorkspaceId, manifest.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationLearningProfile?)null);

        ImprovementAdvisorService sut = new(analyzer.Object, generator.Object, learning.Object);

        ImprovementPlan plan = await sut.GeneratePlanAsync(manifest, findings, CancellationToken.None);

        plan.RunId.Should().Be(manifest.RunId);
        plan.ComparedToRunId.Should().BeNull();
        analyzer.Verify(a => a.Analyze(manifest, findings, null), Times.Once);
        generator.Verify(
            g => g.Generate(It.IsAny<IReadOnlyList<ImprovementSignal>>(), null),
            Times.Once);
    }

    [Fact]
    public async Task GeneratePlanAsync_WithComparison_CallsAnalyzerWithComparison_AndMapsComparedToRunId()
    {
        GoldenManifest manifest = CreateManifest();
        FindingsSnapshot findings = CreateFindings(manifest.RunId);
        Guid baseRunId = Guid.NewGuid();
        ComparisonResult comparison = new()
        {
            BaseRunId = baseRunId,
            TargetRunId = manifest.RunId
        };

        Mock<IImprovementSignalAnalyzer> analyzer = new();
        analyzer
            .Setup(a => a.Analyze(manifest, findings, comparison))
            .Returns([]);

        Mock<IRecommendationGenerator> generator = new();
        generator
            .Setup(g => g.Generate(It.IsAny<IReadOnlyList<ImprovementSignal>>(), It.IsAny<RecommendationLearningProfile?>()))
            .Returns([]);

        Mock<IRecommendationLearningService> learning = new();
        learning
            .Setup(s => s.GetLatestProfileAsync(manifest.TenantId, manifest.WorkspaceId, manifest.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationLearningProfile?)null);

        ImprovementAdvisorService sut = new(analyzer.Object, generator.Object, learning.Object);

        ImprovementPlan plan = await sut.GeneratePlanAsync(manifest, findings, comparison, CancellationToken.None);

        plan.RunId.Should().Be(manifest.RunId);
        plan.ComparedToRunId.Should().Be(baseRunId);
        analyzer.Verify(a => a.Analyze(manifest, findings, comparison), Times.Once);
    }

    [Fact]
    public async Task GeneratePlanAsync_WhenNoRecommendations_SummarySaysNoneIdentified()
    {
        GoldenManifest manifest = CreateManifest();
        FindingsSnapshot findings = CreateFindings(manifest.RunId);

        Mock<IImprovementSignalAnalyzer> analyzer = new();
        analyzer.Setup(a => a.Analyze(manifest, findings, null)).Returns([]);

        Mock<IRecommendationGenerator> generator = new();
        generator.Setup(g => g.Generate(It.IsAny<IReadOnlyList<ImprovementSignal>>(), It.IsAny<RecommendationLearningProfile?>())).Returns([]);

        Mock<IRecommendationLearningService> learning = new();
        learning
            .Setup(s => s.GetLatestProfileAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationLearningProfile?)null);

        ImprovementAdvisorService sut = new(analyzer.Object, generator.Object, learning.Object);

        ImprovementPlan plan = await sut.GeneratePlanAsync(manifest, findings, CancellationToken.None);

        plan.SummaryNotes.Should().ContainSingle().Which.Should().Be("No significant improvements were identified.");
    }

    [Fact]
    public async Task GeneratePlanAsync_WhenProfileExists_SummaryMentionsAdaptivePrioritization()
    {
        GoldenManifest manifest = CreateManifest();
        FindingsSnapshot findings = CreateFindings(manifest.RunId);
        RecommendationLearningProfile profile = new()
        {
            TenantId = manifest.TenantId,
            WorkspaceId = manifest.WorkspaceId,
            ProjectId = manifest.ProjectId
        };

        List<ImprovementRecommendation> recs =
        [
            new ImprovementRecommendation
            {
                Title = "t1",
                Category = "Security",
                Rationale = "r",
                SuggestedAction = "a",
                Urgency = "Low",
                ExpectedImpact = "i"
            }
        ];

        Mock<IImprovementSignalAnalyzer> analyzer = new();
        analyzer.Setup(a => a.Analyze(manifest, findings, null)).Returns([]);

        Mock<IRecommendationGenerator> generator = new();
        generator.Setup(g => g.Generate(It.IsAny<IReadOnlyList<ImprovementSignal>>(), profile)).Returns(recs);

        Mock<IRecommendationLearningService> learning = new();
        learning
            .Setup(s => s.GetLatestProfileAsync(manifest.TenantId, manifest.WorkspaceId, manifest.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        ImprovementAdvisorService sut = new(analyzer.Object, generator.Object, learning.Object);

        ImprovementPlan plan = await sut.GeneratePlanAsync(manifest, findings, CancellationToken.None);

        plan.Recommendations.Should().BeEquivalentTo(recs, o => o.WithStrictOrdering());
        plan.SummaryNotes.Should().Contain(s => s.Contains("Adaptive prioritization", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GeneratePlanAsync_WhenNoProfile_SummaryMentionsBasePrioritization()
    {
        GoldenManifest manifest = CreateManifest();
        FindingsSnapshot findings = CreateFindings(manifest.RunId);

        List<ImprovementRecommendation> recs =
        [
            new ImprovementRecommendation
            {
                Title = "t1",
                Category = "Cost",
                Rationale = "r",
                SuggestedAction = "a",
                Urgency = "High",
                ExpectedImpact = "i"
            }
        ];

        Mock<IImprovementSignalAnalyzer> analyzer = new();
        analyzer.Setup(a => a.Analyze(manifest, findings, null)).Returns([]);

        Mock<IRecommendationGenerator> generator = new();
        generator.Setup(g => g.Generate(It.IsAny<IReadOnlyList<ImprovementSignal>>(), null)).Returns(recs);

        Mock<IRecommendationLearningService> learning = new();
        learning
            .Setup(s => s.GetLatestProfileAsync(manifest.TenantId, manifest.WorkspaceId, manifest.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationLearningProfile?)null);

        ImprovementAdvisorService sut = new(analyzer.Object, generator.Object, learning.Object);

        ImprovementPlan plan = await sut.GeneratePlanAsync(manifest, findings, CancellationToken.None);

        plan.SummaryNotes.Should().Contain(s => s.Contains("No adaptive learning profile", StringComparison.OrdinalIgnoreCase));
        plan.SummaryNotes.Should().Contain(s => s.Contains("1 recommendations are high urgency", StringComparison.OrdinalIgnoreCase));
        plan.SummaryNotes.Should().Contain("Cost: 1");
    }

    private static GoldenManifest CreateManifest()
    {
        return new GoldenManifest
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            ManifestId = Guid.NewGuid(),
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "h",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rsh"
        };
    }

    private static FindingsSnapshot CreateFindings(Guid runId)
    {
        return new FindingsSnapshot
        {
            FindingsSnapshotId = Guid.NewGuid(),
            RunId = runId,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow
        };
    }
}
