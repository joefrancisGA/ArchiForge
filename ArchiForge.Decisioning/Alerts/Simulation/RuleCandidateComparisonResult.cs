namespace ArchiForge.Decisioning.Alerts.Simulation;

public class RuleCandidateComparisonResult
{
    public RuleSimulationResult CandidateA { get; set; } = null!;
    public RuleSimulationResult CandidateB { get; set; } = null!;

    public List<string> SummaryNotes { get; set; } = [];
}
