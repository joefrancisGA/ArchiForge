using ArchiForge.Decisioning.Alerts.Composite;

namespace ArchiForge.Decisioning.Alerts.Simulation;

/// <summary>Input to <see cref="IRuleSimulationService.CompareCandidatesAsync"/> — two rule definitions of the same kind to compare over the same run window.</summary>
public class RuleCandidateComparisonRequest
{
    /// <summary><c>Simple</c> or <c>Composite</c>.</summary>
    public string RuleKind { get; set; } = null!;

    /// <summary>First simple-rule candidate when <see cref="RuleKind"/> is Simple.</summary>
    public AlertRule? CandidateASimpleRule
    {
        get; set;
    }

    /// <summary>Second simple-rule candidate.</summary>
    public AlertRule? CandidateBSimpleRule
    {
        get; set;
    }

    /// <summary>First composite candidate when <see cref="RuleKind"/> is Composite.</summary>
    public CompositeAlertRule? CandidateACompositeRule
    {
        get; set;
    }

    /// <summary>Second composite candidate.</summary>
    public CompositeAlertRule? CandidateBCompositeRule
    {
        get; set;
    }

    /// <summary>Passed through to each nested <see cref="RuleSimulationRequest"/>.</summary>
    public int RecentRunCount { get; set; } = 5;

    /// <summary>Project slug for authority run listing.</summary>
    public string RunProjectSlug { get; set; } = "default";
}
