using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;

namespace ArchiForge.Decisioning.Alerts.Tuning;

public class ThresholdRecommendationRequest
{
    public string RuleKind { get; set; } = null!;

    public AlertRule? BaseSimpleRule { get; set; }
    public CompositeAlertRule? BaseCompositeRule { get; set; }

    public string TunedMetricType { get; set; } = null!;
    public List<decimal> CandidateThresholds { get; set; } = [];

    public int RecentRunCount { get; set; } = 10;

    public int TargetCreatedAlertCountMin { get; set; } = 1;
    public int TargetCreatedAlertCountMax { get; set; } = 5;

    /// <summary>Authority slug for listing runs (same as simulation).</summary>
    public string RunProjectSlug { get; set; } = "default";
}
