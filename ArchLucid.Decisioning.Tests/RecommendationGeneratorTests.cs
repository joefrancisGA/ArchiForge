using ArchLucid.Decisioning.Advisory.Learning;
using ArchLucid.Decisioning.Advisory.Models;
using ArchLucid.Decisioning.Advisory.Services;

using FluentAssertions;

using Moq;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Recommendation Generator.
/// </summary>

[Trait("Suite", "Core")]
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

    [Fact]
    public void Generate_ComplianceGapSignal_MapsTitleAndScorerReceivesMediumUrgency()
    {
        _scorerMock
            .Setup(s => s.Score(It.Is<AdaptiveScoringInput>(i => i.Urgency == "Medium"), It.IsAny<RecommendationLearningProfile?>()))
            .Returns(new AdaptiveScoringResult
            {
                BasePriorityScore = 90,
                AdaptedPriorityScore = 90,
                CategoryWeight = 1.0,
                UrgencyWeight = 1.0,
                SignalTypeWeight = 1.0
            });

        RecommendationGenerator sut = BuildSut();

        ImprovementSignal signal = new()
        {
            SignalType = ImprovementSignalTypes.ComplianceGap,
            Category = ImprovementSignalCategories.Compliance,
            Title = "Control gap",
            Description = "d",
            Severity = ImprovementSignalSeverities.Medium
        };

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate([signal]);

        result.Should().ContainSingle();
        result[0].Title.Should().Be("Address a compliance control gap");
        result[0].Urgency.Should().Be("Medium");
    }

    [Fact]
    public void Generate_CostTopologyAndRegressionSignals_MapKnownTitles()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate(
        [
            new()
            {
                SignalType = ImprovementSignalTypes.TopologyGap,
                Category = ImprovementSignalCategories.Topology,
                Title = "T",
                Description = "d",
                Severity = "Low"
            },
            new()
            {
                SignalType = ImprovementSignalTypes.CostRisk,
                Category = ImprovementSignalCategories.Cost,
                Title = "C",
                Description = "d",
                Severity = ImprovementSignalSeverities.Medium
            },
            new()
            {
                SignalType = ImprovementSignalTypes.SecurityRegression,
                Category = ImprovementSignalCategories.Security,
                Title = "R",
                Description = "d",
                Severity = ImprovementSignalSeverities.Critical
            }
        ]);

        result.Select(r => r.Title).Should()
            .Equal("Reverse a security regression", "Improve topology completeness", "Reduce a cost risk");
    }

    [Fact]
    public void Generate_UnresolvedIssueAndDecisionRemoved_MapTitles()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate(
        [
            new()
            {
                SignalType = ImprovementSignalTypes.UnresolvedIssue,
                Category = ImprovementSignalCategories.Risk,
                Title = "ISS-1",
                Description = "d",
                Severity = ImprovementSignalSeverities.High
            },
            new()
            {
                SignalType = ImprovementSignalTypes.DecisionRemoved,
                Category = ImprovementSignalCategories.Requirement,
                Title = "Removed",
                Description = "d",
                Severity = ImprovementSignalSeverities.High
            },
            new()
            {
                SignalType = ImprovementSignalTypes.CostIncrease,
                Category = ImprovementSignalCategories.Cost,
                Title = "x",
                Description = "d",
                Severity = ImprovementSignalSeverities.Medium
            }
        ]);

        string[] titles = result.Select(r => r.Title).ToArray();
        titles.Should().Contain("Resolve: ISS-1");
        titles.Should().Contain("Restore or replace removed architecture decision");
        titles.Should().Contain("Reduce increased projected cost");
    }

    [Fact]
    public void Generate_UnknownCategory_UsesGenericImpact()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        ImprovementSignal signal = new()
        {
            SignalType = "Custom",
            Category = "Sustainability",
            Title = "T",
            Description = "d",
            Severity = "Low"
        };

        IReadOnlyList<ImprovementRecommendation> result = sut.Generate([signal]);

        result.Single().ExpectedImpact.Should().Be("Improves architecture quality.");
    }

    [Fact]
    public void Generate_SameScore_OrdersByTitleCaseInsensitively()
    {
        SetupScorerPassThrough();
        RecommendationGenerator sut = BuildSut();

        // Use two unknown signal types so recommendation titles come from the signal and differ; score is identical.
        IReadOnlyList<ImprovementRecommendation> result = sut.Generate(
        [
            new()
            {
                SignalType = "Zeta",
                Category = "Sustainability",
                Title = "Bravo",
                Description = "d1",
                Severity = ImprovementSignalSeverities.Critical
            },
            new()
            {
                SignalType = "Zeta",
                Category = "Sustainability",
                Title = "alpha",
                Description = "d2",
                Severity = ImprovementSignalSeverities.Critical
            }
        ]);

        result[0].Title.Should().Be("alpha");
        result[0].Rationale.Should().Be("d2");
        result[1].Title.Should().Be("Bravo");
        result[1].Rationale.Should().Be("d1");
    }

    [Fact]
    public void Generate_Passes_learning_profile_to_scorer()
    {
        RecommendationLearningProfile profile = new();
        _scorerMock
            .Setup(s => s.Score(It.IsAny<AdaptiveScoringInput>(), profile))
            .Returns(
                new AdaptiveScoringResult
                {
                    BasePriorityScore = 1,
                    AdaptedPriorityScore = 42,
                    CategoryWeight = 1.0,
                    UrgencyWeight = 1.0,
                    SignalTypeWeight = 1.0
                });

        RecommendationGenerator sut = BuildSut();
        IReadOnlyList<ImprovementRecommendation> result = sut.Generate(
        [
            new()
            {
                SignalType = ImprovementSignalTypes.CostRisk,
                Category = ImprovementSignalCategories.Cost,
                Title = "t",
                Description = "d",
                Severity = ImprovementSignalSeverities.Medium
            }
        ],
            profile);

        result.Single().PriorityScore.Should().Be(42);
    }
}
