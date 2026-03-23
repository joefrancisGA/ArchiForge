using ArchiForge.Decisioning.Alerts.Simulation;

namespace ArchiForge.Decisioning.Alerts.Tuning;

/// <summary>
/// Heuristic scorer for a single <see cref="RuleSimulationResult"/> against target alert volume bounds.
/// </summary>
public interface IAlertNoiseScorer
{
    /// <summary>
    /// Produces coverage, penalty, and final score components; higher <see cref="NoiseScoreBreakdown.FinalScore"/> is better.
    /// </summary>
    NoiseScoreBreakdown Score(
        RuleSimulationResult simulationResult,
        int targetCreatedAlertCountMin,
        int targetCreatedAlertCountMax);
}
