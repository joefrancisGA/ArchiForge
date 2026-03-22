using ArchiForge.Decisioning.Alerts.Simulation;

namespace ArchiForge.Decisioning.Alerts.Tuning;

public class ThresholdCandidateEvaluation
{
    public ThresholdCandidate Candidate { get; set; } = null!;
    public RuleSimulationResult SimulationResult { get; set; } = null!;
    public NoiseScoreBreakdown ScoreBreakdown { get; set; } = null!;
}
