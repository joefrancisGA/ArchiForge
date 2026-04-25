namespace ArchLucid.Decisioning.Alerts.Composite;

/// <summary>
///     String ids for <see cref="AlertRuleCondition.MetricType" />; must align with
///     <see cref="CompositeAlertRuleEvaluator" /> / <see cref="AlertMetricSnapshotBuilder" /> resolution.
/// </summary>
public static class AlertMetricType
{
    public const string CriticalRecommendationCount = "CriticalRecommendationCount";
    public const string NewComplianceGapCount = "NewComplianceGapCount";
    public const string CostIncreasePercent = "CostIncreasePercent";
    public const string DeferredHighPriorityRecommendationCount = "DeferredHighPriorityRecommendationCount";
    public const string RejectedSecurityRecommendationCount = "RejectedSecurityRecommendationCount";
    public const string AcceptanceRatePercent = "AcceptanceRatePercent";
}
