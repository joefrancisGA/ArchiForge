namespace ArchiForge.Decisioning.Alerts.Simulation;

/// <summary>Side-by-side <see cref="RuleSimulationResult"/> from <see cref="IRuleSimulationService.CompareCandidatesAsync"/>.</summary>
public class RuleCandidateComparisonResult
{
    /// <summary>Simulation for candidate A.</summary>
    public RuleSimulationResult CandidateA { get; set; } = null!;

    /// <summary>Simulation for candidate B.</summary>
    public RuleSimulationResult CandidateB { get; set; } = null!;

    /// <summary>Cross-candidate summary lines (e.g. would-create counts).</summary>
    public List<string> SummaryNotes { get; set; } = [];
}
