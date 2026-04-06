namespace ArchiForge.Decisioning.Alerts.Composite;

/// <summary>
/// Numeric metrics derived from an <see cref="AlertEvaluationContext"/> for composite rule predicates (<see cref="AlertRuleCondition"/>).
/// </summary>
/// <remarks>
/// Built by <see cref="AlertMetricSnapshotBuilder"/> immediately before <see cref="ICompositeAlertRuleEvaluator"/> and <see cref="IAlertSuppressionPolicy"/>.
/// </remarks>
public class AlertMetricSnapshot
{
    /// <summary>Count of recommendations with urgency Critical or High on the improvement plan.</summary>
    public decimal CriticalRecommendationCount { get; set; }

    /// <summary>Count of security-related changes on the comparison result (used as a compliance-gap proxy).</summary>
    public decimal NewComplianceGapCount { get; set; }

    /// <summary>Percent cost increase from baseline to target when comparison exposes cost deltas; otherwise 0.</summary>
    public decimal CostIncreasePercent { get; set; }

    /// <summary>Deferred recommendations with priority score ≥ 80.</summary>
    public decimal DeferredHighPriorityRecommendationCount { get; set; }

    /// <summary>Rejected recommendations in category Security.</summary>
    public decimal RejectedSecurityRecommendationCount { get; set; }

    /// <summary>Accepted ÷ proposed across learning profile categories; 0 when profile is missing or no proposals.</summary>
    public decimal AcceptanceRatePercent { get; set; }
}
