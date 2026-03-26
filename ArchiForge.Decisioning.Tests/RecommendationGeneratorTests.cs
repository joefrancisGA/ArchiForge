using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Services;

using FluentAssertions;

using Moq;

namespace ArchiForge.Decisioning.Tests;

public sealed class RecommendationGeneratorTests
{
    private readonly Mock<IAdaptiveRecommendationScorer> _scorerMock = new(MockBehavior.Strict);

    private RecommendationGenerator BuildSut() => new(_scorerMock.Object);

    /// <summary>
    /// Returns a fixed AdaptiveScoringResult with the adapted score equal to the base score
    /// so tests can reason purely about ordering and field mapping without scoring noise.
    /// </summary>
    private void SetupScorerPassThrough()
    {
        _scorerMock
            .Setup(s => s.Score(It.IsAny<AdaptiveScoringInput>(), It.IsAny<RecommendationLearningProfile?>()))
            .Returns<AdaptiveScoringInput, RecommendationLearningProfile?>((input, _) =>
                new AdaptiveScoringResult
                {
                    BasePriorityScore = input.BasePriorityScore,
                    AdaptedPriorityScore = input.BasePriorityScore,
                    CategoryWeight = 1.0,
                    UrgencyWeight = 1.0,
                    SignalTypeWeight = 1.0
                });
    }

    [Fact]
    public void Generate_NullSignals_Throws()
    {
        RecommendationGenerator sut = BuildSut();

        Action act = () => sut.Generate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Generate_EmptySignals_ReturnsEmptyList()
    {
        RecommendationGenerator sut = BuildSut();

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Generate_SecurityGapSignal_MapsToCorrectTitle()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        ImprovementSignal signal = new()
        {
            SignalType = "SecurityGap",
            Category = "Security",
            Title = "Security protection gap",
            Description = "Storage account not encrypted",
            Severity = "High"
        };

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate([signal]);

        result.Should().ContainSingle()
            .Which.Title.Should().Be("Close a security protection gap");
    }

    [Fact]
    public void Generate_UncoveredRequirementSignal_MapsToCorrectTitle()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        ImprovementSignal signal = new()
        {
            SignalType = "UncoveredRequirement",
            Category = "Requirement",
            Title = "Requirement not covered: REQ-001",
            Description = "REQ-001",
            Severity = "High"
        };

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate([signal]);

        result.Should().ContainSingle()
            .Which.Title.Should().Be("Cover an uncovered requirement");
    }

    [Fact]
    public void Generate_PolicyViolationSignal_MapsToCorrectTitle()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        ImprovementSignal signal = new()
        {
            SignalType = ImprovementSignalTypes.PolicyViolation,
            Category = ImprovementSignalCategories.Compliance,
            Title = "Policy violation: X",
            Description = "desc",
            Severity = ImprovementSignalSeverities.High
        };

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate([signal]);

        result.Should().ContainSingle()
            .Which.Title.Should().Be("Resolve a manifest policy violation");
    }

    [Fact]
    public void Generate_UnknownSignalType_UsesTitleFromSignal()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        ImprovementSignal signal = new()
        {
            SignalType = "SomeFutureType",
            Category = "Risk",
            Title = "Unusual signal",
            Description = "something new",
            Severity = "Low"
        };

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate([signal]);

        result.Should().ContainSingle()
            .Which.Title.Should().Be("Unusual signal");
    }

    [Fact]
    public void Generate_RecommendationsOrderedByPriorityScoreDescending()
    {
        _scorerMock
            .Setup(s => s.Score(It.Is<AdaptiveScoringInput>(i => i.Category == "Security"), It.IsAny<RecommendationLearningProfile?>()))
            .Returns(new AdaptiveScoringResult { BasePriorityScore = 100, AdaptedPriorityScore = 100 });

        _scorerMock
            .Setup(s => s.Score(It.Is<AdaptiveScoringInput>(i => i.Category == "Cost"), It.IsAny<RecommendationLearningProfile?>()))
            .Returns(new AdaptiveScoringResult { BasePriorityScore = 60, AdaptedPriorityScore = 60 });

        RecommendationGenerator sut = BuildSut();

        List<ImprovementSignal> signals =
        [
            new() { SignalType = "CostRisk", Category = "Cost", Title = "Cost risk", Description = "d", Severity = "Medium" },
            new() { SignalType = "SecurityGap", Category = "Security", Title = "Security gap", Description = "d", Severity = "High" }
        ];

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate(signals);

        result.Should().HaveCount(2);
        result[0].PriorityScore.Should().Be(100);
        result[1].PriorityScore.Should().Be(60);
    }

    [Fact]
    public void Generate_FindingIdsPropagatedToRecommendation()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        ImprovementSignal signal = new()
        {
            SignalType = "ComplianceGap",
            Category = "Compliance",
            Title = "Gap",
            Description = "d",
            Severity = "High",
            FindingIds = ["finding-1", "finding-2"]
        };

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate([signal]);

        result.Single().SupportingFindingIds.Should().Equal("finding-1", "finding-2");
    }

    [Fact]
    public void Generate_RecommendationUrgency_MappedFromSeverity()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        ImprovementSignal signal = new()
        {
            SignalType = "TopologyGap",
            Category = "Topology",
            Title = "Gap",
            Description = "d",
            Severity = "Critical"
        };

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate([signal]);

        result.Single().Urgency.Should().Be("Critical");
    }

    [Fact]
    public void Generate_SecurityCategory_ImpactDescribesSecurityExposure()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        ImprovementSignal signal = new()
        {
            SignalType = "SecurityGap",
            Category = "Security",
            Title = "Gap",
            Description = "d",
            Severity = "High"
        };

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate([signal]);

        result.Single().ExpectedImpact.Should().Contain("security");
    }
}
