namespace ArchiForge.Decisioning.Alerts.Simulation;

public class RuleCandidateComparisonResult
{
    public RuleSimulationResult CandidateA { get; set; } = default!;
    public RuleSimulationResult CandidateB { get; set; } = default!;

    public List<string> SummaryNotes { get; set; } = [];
}
