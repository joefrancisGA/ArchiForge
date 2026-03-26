using ArchiForge.Decisioning.Advisory.Learning;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class AdaptiveRecommendationScorerTests
{
    private readonly AdaptiveRecommendationScorer _sut = new();

    private static AdaptiveScoringInput BaseInput(int basePriority = 80) => new()
    {
        Category = "Security",
        Urgency = "High",
        SignalType = "SecurityGap",
        BasePriorityScore = basePriority
    };

    [Fact]
    public void Score_NullProfile_ReturnsBaseScore()
    {
        AdaptiveScoringResult result = _sut.Score(BaseInput(70), null);

        result.AdaptedPriorityScore.Should().Be(70);
        result.BasePriorityScore.Should().Be(70);
        result.CategoryWeight.Should().Be(1.0);
        result.UrgencyWeight.Should().Be(1.0);
        result.SignalTypeWeight.Should().Be(1.0);
        result.Notes.Should().ContainSingle()
            .Which.Should().Contain("No learning profile");
    }

    [Fact]
    public void Score_ProfileWithCategoryWeight_AffectsScore()
    {
        RecommendationLearningProfile profile = new();
        profile.CategoryWeights["Security"] = 1.5;

        AdaptiveScoringResult result = _sut.Score(BaseInput(100), profile);

        result.CategoryWeight.Should().Be(1.5);
        result.AdaptedPriorityScore.Should().Be(150);
        result.Notes.Should().Contain(n => n.Contains("Security"));
    }

    [Fact]
    public void Score_ProfileWithUrgencyWeight_AffectsScore()
    {
        RecommendationLearningProfile profile = new();
        profile.UrgencyWeights["High"] = 2.0;

        AdaptiveScoringResult result = _sut.Score(BaseInput(50), profile);

        result.UrgencyWeight.Should().Be(2.0);
        result.AdaptedPriorityScore.Should().Be(100);
    }

    [Fact]
    public void Score_ProfileWithSignalTypeWeight_AffectsScore()
    {
        RecommendationLearningProfile profile = new();
        profile.SignalTypeWeights["SecurityGap"] = 1.25;

        AdaptiveScoringResult result = _sut.Score(BaseInput(80), profile);

        result.SignalTypeWeight.Should().Be(1.25);
        result.AdaptedPriorityScore.Should().Be(100);
    }

    [Fact]
    public void Score_AllWeightsApplied_MultipliesCorrectly()
    {
        RecommendationLearningProfile profile = new();
        profile.CategoryWeights["Security"] = 2.0;
        profile.UrgencyWeights["High"] = 1.5;
        profile.SignalTypeWeights["SecurityGap"] = 0.5;

        AdaptiveScoringResult result = _sut.Score(BaseInput(100), profile);

        // 100 * 2.0 * 1.5 * 0.5 = 150
        result.AdaptedPriorityScore.Should().Be(150);
    }

    [Fact]
    public void Score_MissingCategoryWeight_UsesDefaultOne()
    {
        RecommendationLearningProfile profile = new();

        AdaptiveScoringResult result = _sut.Score(BaseInput(60), profile);

        result.CategoryWeight.Should().Be(1.0);
        result.UrgencyWeight.Should().Be(1.0);
        result.SignalTypeWeight.Should().Be(1.0);
        result.AdaptedPriorityScore.Should().Be(60);
    }

    [Fact]
    public void Score_NullSignalType_DoesNotApplySignalWeight()
    {
        RecommendationLearningProfile profile = new();
        profile.SignalTypeWeights["SecurityGap"] = 2.0;

        AdaptiveScoringInput input = new()
        {
            Category = "Security",
            Urgency = "High",
            SignalType = null,
            BasePriorityScore = 50
        };

        AdaptiveScoringResult result = _sut.Score(input, profile);

        result.SignalTypeWeight.Should().Be(1.0);
        result.AdaptedPriorityScore.Should().Be(50);
    }

    [Fact]
    public void Score_MidpointRounding_RoundsAwayFromZero()
    {
        // 0.5 base * 1.0 all weights rounds to 1, not 0
        RecommendationLearningProfile profile = new();

        AdaptiveScoringInput input = BaseInput(0);

        AdaptiveScoringResult result = _sut.Score(input, profile);

        result.AdaptedPriorityScore.Should().Be(0);
    }
}
