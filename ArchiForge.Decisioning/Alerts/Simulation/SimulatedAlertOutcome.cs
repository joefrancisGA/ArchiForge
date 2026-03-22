namespace ArchiForge.Decisioning.Alerts.Simulation;

public class SimulatedAlertOutcome
{
    public Guid? RunId { get; set; }
    public Guid? ComparedToRunId { get; set; }

    public bool RuleMatched { get; set; }
    public bool WouldCreateAlert { get; set; }
    public bool WouldBeSuppressed { get; set; }

    public string Title { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public string Description { get; set; } = null!;

    public string DeduplicationKey { get; set; } = null!;
    public string SuppressionReason { get; set; } = null!;

    public string EvaluationMode { get; set; } = null!;
    public List<string> Notes { get; set; } = [];
}
