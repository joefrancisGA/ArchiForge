using ArchLucid.Decisioning.Alerts.Simulation;

namespace ArchLucid.Decisioning.Alerts.Tuning;

/// <summary>One row in <see cref="ThresholdRecommendationResult.Candidates" />: candidate + simulation + noise score.</summary>
public class ThresholdCandidateEvaluation
{
    /// <summary>Threshold that was applied for this run.</summary>
    public ThresholdCandidate Candidate
    {
        get;
        set;
    } = null!;

    /// <summary>Full simulation output for that threshold.</summary>
    public RuleSimulationResult SimulationResult
    {
        get;
        set;
    } = null!;

    /// <summary>Heuristic score from <see cref="IAlertNoiseScorer" />.</summary>
    public NoiseScoreBreakdown ScoreBreakdown
    {
        get;
        set;
    } = null!;
}
