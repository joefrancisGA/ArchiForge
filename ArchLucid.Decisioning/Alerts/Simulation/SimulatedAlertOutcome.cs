namespace ArchiForge.Decisioning.Alerts.Simulation;

/// <summary>One row in <see cref="RuleSimulationResult.Outcomes"/> for a single run context.</summary>
public class SimulatedAlertOutcome
{
    /// <summary>Run evaluated for this row.</summary>
    public Guid? RunId { get; set; }

    /// <summary>Comparison baseline when applicable.</summary>
    public Guid? ComparedToRunId { get; set; }

    /// <summary>Whether the rule’s predicate matched.</summary>
    public bool RuleMatched { get; set; }

    /// <summary>For composite: mirrors suppression <c>ShouldCreateAlert</c>; for simple: true when an alert DTO was generated.</summary>
    public bool WouldCreateAlert { get; set; }

    /// <summary>Typically inverse of <see cref="WouldCreateAlert"/> when matched.</summary>
    public bool WouldBeSuppressed { get; set; }

    /// <summary>Title template or rule name for display.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Severity carried from the rule.</summary>
    public string Severity { get; set; } = null!;

    /// <summary>Explanation or suppression reason text.</summary>
    public string Description { get; set; } = null!;

    /// <summary>Dedupe key when composite suppression ran; empty for simple non-match.</summary>
    public string DeduplicationKey { get; set; } = null!;

    /// <summary>Suppression policy explanation when applicable.</summary>
    public string SuppressionReason { get; set; } = null!;

    /// <summary><c>Simple</c> or <c>Composite</c> evaluation path.</summary>
    public string EvaluationMode { get; set; } = null!;

    /// <summary>Additional diagnostic bullets.</summary>
    public List<string> Notes { get; set; } = [];
}
