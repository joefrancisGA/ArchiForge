namespace ArchiForge.Decisioning.Alerts.Simulation;

/// <summary>Aggregated output of <see cref="IRuleSimulationService.SimulateAsync"/>.</summary>
public class RuleSimulationResult
{
    /// <summary>Echo of request rule kind.</summary>
    public string RuleKind { get; set; } = null!;

    /// <summary>UTC timestamp when simulation finished.</summary>
    public DateTime SimulatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Number of evaluation contexts processed.</summary>
    public int EvaluatedRunCount { get; set; }

    /// <summary>Contexts where the rule predicate matched.</summary>
    public int MatchedCount { get; set; }

    /// <summary>Matched outcomes that would insert a new alert (composite: <see cref="SimulatedAlertOutcome.WouldCreateAlert"/>).</summary>
    public int WouldCreateCount { get; set; }

    /// <summary>Matched outcomes that would be dropped by suppression (composite) or zero for non-matches.</summary>
    public int WouldSuppressCount { get; set; }

    /// <summary>Human-readable rollup lines.</summary>
    public List<string> SummaryNotes { get; set; } = [];

    /// <summary>Per-context detail rows.</summary>
    public List<SimulatedAlertOutcome> Outcomes { get; set; } = [];
}
