using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Alerts.Simulation;

namespace ArchiForge.Decisioning.Alerts.Tuning;

/// <summary>Request to sweep candidate thresholds for one tunable metric on a base simple or composite rule.</summary>
/// <remarks>Validated by <c>ThresholdRecommendationRequestValidator</c> in the API.</remarks>
public class ThresholdRecommendationRequest
{
    /// <summary><c>Simple</c> or <c>Composite</c>.</summary>
    public string RuleKind { get; set; } = null!;

    /// <summary>Template simple rule when <see cref="RuleKind"/> is Simple.</summary>
    public AlertRule? BaseSimpleRule
    {
        get; set;
    }

    /// <summary>Template composite rule when <see cref="RuleKind"/> is Composite.</summary>
    public CompositeAlertRule? BaseCompositeRule
    {
        get; set;
    }

    /// <summary>Which condition metric to rewrite (must exist on composite rules; simple rules are aligned by service).</summary>
    public string TunedMetricType { get; set; } = null!;

    /// <summary>Threshold values to simulate (distinct, ordered ascending in service).</summary>
    public List<decimal> CandidateThresholds { get; set; } = [];

    /// <summary>Window size passed to <see cref="RuleSimulationRequest.RecentRunCount"/>.</summary>
    public int RecentRunCount { get; set; } = 10;

    /// <summary>Lower bound for acceptable “would create” count per simulation.</summary>
    public int TargetCreatedAlertCountMin { get; set; } = 1;

    /// <summary>Upper bound for acceptable “would create” count per simulation.</summary>
    public int TargetCreatedAlertCountMax { get; set; } = 5;

    /// <summary>Authority slug for listing runs (same as simulation).</summary>
    public string RunProjectSlug { get; set; } = "default";
}
