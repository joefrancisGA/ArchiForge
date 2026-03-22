using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;

namespace ArchiForge.Decisioning.Alerts.Simulation;

public class RuleSimulationRequest
{
    /// <summary>Simple or Composite</summary>
    public string RuleKind { get; set; } = null!;

    public AlertRule? SimpleRule { get; set; }
    public CompositeAlertRule? CompositeRule { get; set; }

    public Guid? RunId { get; set; }
    public Guid? ComparedToRunId { get; set; }

    public int RecentRunCount { get; set; } = 5;

    /// <summary>When false and <see cref="RunId"/> is null, returns no contexts (reserved for future single-current-run modes).</summary>
    public bool UseHistoricalWindow { get; set; } = true;

    /// <summary>Authority slug for listing runs (e.g. scheduled advisory scans).</summary>
    public string RunProjectSlug { get; set; } = "default";
}
