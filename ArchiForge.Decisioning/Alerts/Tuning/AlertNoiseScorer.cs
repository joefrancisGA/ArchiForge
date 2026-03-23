using ArchiForge.Decisioning.Alerts.Simulation;

namespace ArchiForge.Decisioning.Alerts.Tuning;

/// <summary>Default <see cref="IAlertNoiseScorer"/> using coverage, target-band, suppression ratio, and alert density heuristics.</summary>
public sealed class AlertNoiseScorer : IAlertNoiseScorer
{
    /// <inheritdoc />
    public NoiseScoreBreakdown Score(
        RuleSimulationResult simulationResult,
        int targetCreatedAlertCountMin,
        int targetCreatedAlertCountMax)
    {
        var result = new NoiseScoreBreakdown();

        var evaluated = Math.Max(1, simulationResult.EvaluatedRunCount);
        var created = simulationResult.WouldCreateCount;
        var suppressed = simulationResult.WouldSuppressCount;
        var matched = simulationResult.MatchedCount;

        result.CoverageScore = matched == 0
            ? 0
            : Math.Min(40, matched * 5);

        if (created < targetCreatedAlertCountMin)
        {
            result.NoisePenalty = (targetCreatedAlertCountMin - created) * 8;
            result.Notes.Add("Candidate may be too insensitive and could miss important events.");
        }
        else if (created > targetCreatedAlertCountMax)
        {
            result.NoisePenalty = (created - targetCreatedAlertCountMax) * 10;
            result.Notes.Add("Candidate may produce too many alerts and increase operator noise.");
        }

        var suppressionRatio = (double)suppressed / evaluated;
        result.SuppressionPenalty = suppressionRatio * 15;

        if (suppressionRatio > 0.5)
        {
            result.Notes.Add("A large share of matched outcomes would be suppressed, which suggests redundant triggering.");
        }

        var density = (double)created / evaluated;
        result.DensityPenalty = density > 1.0 ? (density - 1.0) * 20 : 0;

        if (density > 1.0)
        {
            result.Notes.Add("Average alert density exceeds one alert per evaluated run.");
        }

        result.FinalScore =
            result.CoverageScore
            - result.NoisePenalty
            - result.SuppressionPenalty
            - result.DensityPenalty;

        result.Notes.Add($"Coverage score: {result.CoverageScore:0.##}");
        result.Notes.Add($"Noise penalty: {result.NoisePenalty:0.##}");
        result.Notes.Add($"Suppression penalty: {result.SuppressionPenalty:0.##}");
        result.Notes.Add($"Density penalty: {result.DensityPenalty:0.##}");
        result.Notes.Add($"Final score: {result.FinalScore:0.##}");

        return result;
    }
}
