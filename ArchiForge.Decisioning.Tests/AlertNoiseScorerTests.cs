using ArchiForge.Decisioning.Alerts.Simulation;
using ArchiForge.Decisioning.Alerts.Tuning;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// Unit tests for <see cref="AlertNoiseScorer"/>: verifies that each scoring component
/// (CoverageScore, NoisePenalty, SuppressionPenalty, DensityPenalty) is calculated
/// correctly in isolation and in combination.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AlertNoiseScorerTests
{
    private readonly AlertNoiseScorer _sut = new();

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static RuleSimulationResult MakeResult(
        int evaluated = 10,
        int matched = 0,
        int wouldCreate = 0,
        int wouldSuppress = 0) => new()
    {
        EvaluatedRunCount = evaluated,
        MatchedCount = matched,
        WouldCreateCount = wouldCreate,
        WouldSuppressCount = wouldSuppress
    };

    // ──────────────────────────────────────────────────────────────────────────
    // CoverageScore
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Score_ZeroMatches_CoverageScoreIsZero()
    {
        RuleSimulationResult result = MakeResult(evaluated: 10, matched: 0, wouldCreate: 0);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 1, targetCreatedAlertCountMax: 3);

        score.CoverageScore.Should().Be(0);
    }

    [Fact]
    public void Score_FewMatches_CoverageScoreScalesWithCount()
    {
        // matched=4 → min(40, 4*5) = 20
        RuleSimulationResult result = MakeResult(evaluated: 10, matched: 4, wouldCreate: 2);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 1, targetCreatedAlertCountMax: 5);

        score.CoverageScore.Should().Be(20);
    }

    [Fact]
    public void Score_ManyMatches_CoverageScoreCapsAt40()
    {
        // matched=20 → min(40, 20*5) = 40
        RuleSimulationResult result = MakeResult(evaluated: 20, matched: 20, wouldCreate: 5);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 1, targetCreatedAlertCountMax: 10);

        score.CoverageScore.Should().Be(40);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NoisePenalty — too few alerts
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Score_CreatedBelowTargetMin_NoisePenaltyApplied()
    {
        // wouldCreate=1, targetMin=3 → (3-1)*8 = 16
        RuleSimulationResult result = MakeResult(evaluated: 10, matched: 5, wouldCreate: 1);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 3, targetCreatedAlertCountMax: 5);

        score.NoisePenalty.Should().Be(16);
        score.Notes.Should().Contain(n => n.Contains("too insensitive"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NoisePenalty — too many alerts
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Score_CreatedAboveTargetMax_NoisePenaltyApplied()
    {
        // wouldCreate=8, targetMax=5 → (8-5)*10 = 30
        RuleSimulationResult result = MakeResult(evaluated: 10, matched: 8, wouldCreate: 8);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 1, targetCreatedAlertCountMax: 5);

        score.NoisePenalty.Should().Be(30);
        score.Notes.Should().Contain(n => n.Contains("too many alerts"));
    }

    [Fact]
    public void Score_CreatedWithinTargetBand_NoPenalty()
    {
        RuleSimulationResult result = MakeResult(evaluated: 10, matched: 3, wouldCreate: 3);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 1, targetCreatedAlertCountMax: 5);

        score.NoisePenalty.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SuppressionPenalty
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Score_HighSuppressionRatio_PenaltyAppliedAndNoteAdded()
    {
        // wouldSuppress=6 out of evaluated=10 → ratio=0.6 → 0.6*15=9
        RuleSimulationResult result = MakeResult(evaluated: 10, matched: 8, wouldCreate: 2, wouldSuppress: 6);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 1, targetCreatedAlertCountMax: 5);

        score.SuppressionPenalty.Should().BeApproximately(9, 0.01);
        score.Notes.Should().Contain(n => n.Contains("suppressed"));
    }

    [Fact]
    public void Score_NoSuppression_NoPenalty()
    {
        RuleSimulationResult result = MakeResult(evaluated: 10, matched: 2, wouldCreate: 2, wouldSuppress: 0);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 1, targetCreatedAlertCountMax: 5);

        score.SuppressionPenalty.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DensityPenalty
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Score_DensityAboveOne_PenaltyApplied()
    {
        // wouldCreate=20, evaluated=10 → density=2.0 → (2-1)*20=20
        RuleSimulationResult result = MakeResult(evaluated: 10, matched: 20, wouldCreate: 20);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 5, targetCreatedAlertCountMax: 25);

        score.DensityPenalty.Should().BeApproximately(20, 0.01);
        score.Notes.Should().Contain(n => n.Contains("density"));
    }

    [Fact]
    public void Score_DensityAtOrBelowOne_NoPenalty()
    {
        // wouldCreate=10, evaluated=10 → density=1.0 → penalty=0
        RuleSimulationResult result = MakeResult(evaluated: 10, matched: 10, wouldCreate: 10);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 5, targetCreatedAlertCountMax: 15);

        score.DensityPenalty.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FinalScore arithmetic
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Score_FinalScore_EqualsCoverageMinusPenalties()
    {
        // matched=3, wouldCreate=3 (in band [1,5]) → coverage=15, noise=0, suppression=0, density=0
        RuleSimulationResult result = MakeResult(evaluated: 10, matched: 3, wouldCreate: 3, wouldSuppress: 0);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 1, targetCreatedAlertCountMax: 5);

        score.FinalScore.Should().BeApproximately(
            score.CoverageScore - score.NoisePenalty - score.SuppressionPenalty - score.DensityPenalty,
            precision: 0.001);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Summary notes are always populated
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Score_AlwaysEmitsSummaryNotesForComponents()
    {
        RuleSimulationResult result = MakeResult(evaluated: 5, matched: 2, wouldCreate: 2);

        NoiseScoreBreakdown score = _sut.Score(result, targetCreatedAlertCountMin: 1, targetCreatedAlertCountMax: 4);

        score.Notes.Should().Contain(n => n.Contains("Coverage score"));
        score.Notes.Should().Contain(n => n.Contains("Noise penalty"));
        score.Notes.Should().Contain(n => n.Contains("Suppression penalty"));
        score.Notes.Should().Contain(n => n.Contains("Density penalty"));
        score.Notes.Should().Contain(n => n.Contains("Final score"));
    }
}
