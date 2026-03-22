namespace ArchiForge.Decisioning.Alerts.Simulation;

public class RuleSimulationResult
{
    public string RuleKind { get; set; } = null!;
    public DateTime SimulatedUtc { get; set; } = DateTime.UtcNow;

    public int EvaluatedRunCount { get; set; }
    public int MatchedCount { get; set; }
    public int WouldCreateCount { get; set; }
    public int WouldSuppressCount { get; set; }

    public List<string> SummaryNotes { get; set; } = [];
    public List<SimulatedAlertOutcome> Outcomes { get; set; } = [];
}
