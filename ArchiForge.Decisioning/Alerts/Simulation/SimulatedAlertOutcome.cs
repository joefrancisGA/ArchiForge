namespace ArchiForge.Decisioning.Alerts.Simulation;

public class SimulatedAlertOutcome
{
    public Guid? RunId { get; set; }
    public Guid? ComparedToRunId { get; set; }

    public bool RuleMatched { get; set; }
    public bool WouldCreateAlert { get; set; }
    public bool WouldBeSuppressed { get; set; }

    public string Title { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public string Description { get; set; } = default!;

    public string DeduplicationKey { get; set; } = default!;
    public string SuppressionReason { get; set; } = default!;

    public string EvaluationMode { get; set; } = default!;
    public List<string> Notes { get; set; } = [];
}
