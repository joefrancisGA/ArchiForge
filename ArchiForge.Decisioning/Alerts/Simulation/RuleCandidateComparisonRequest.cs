using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;

namespace ArchiForge.Decisioning.Alerts.Simulation;

public class RuleCandidateComparisonRequest
{
    public string RuleKind { get; set; } = default!;

    public AlertRule? CandidateA_SimpleRule { get; set; }
    public AlertRule? CandidateB_SimpleRule { get; set; }

    public CompositeAlertRule? CandidateA_CompositeRule { get; set; }
    public CompositeAlertRule? CandidateB_CompositeRule { get; set; }

    public int RecentRunCount { get; set; } = 5;

    public string RunProjectSlug { get; set; } = "default";
}
