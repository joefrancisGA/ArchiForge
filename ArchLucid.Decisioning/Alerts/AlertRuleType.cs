namespace ArchLucid.Decisioning.Alerts;

/// <summary>
///     String values for <see cref="AlertRule.RuleType" />; each maps to a branch in <see cref="AlertEvaluator" />.
/// </summary>
public static class AlertRuleType
{
    /// <summary>Count of critical/high urgency recommendations on the plan.</summary>
    public const string CriticalRecommendationCount = "CriticalRecommendationCount";

    /// <summary>Security-related deltas from comparison (compliance-gap style signal).</summary>
    public const string NewComplianceGapCount = "NewComplianceGapCount";

    /// <summary>Percent cost increase from baseline manifest comparison.</summary>
    public const string CostIncreasePercent = "CostIncreasePercent";

    /// <summary>Age in days of deferred high-priority recommendations.</summary>
    public const string DeferredHighPriorityRecommendationAgeDays = "DeferredHighPriorityRecommendationAgeDays";

    /// <summary>Rejected recommendations in security category.</summary>
    public const string RejectedSecurityRecommendation = "RejectedSecurityRecommendation";

    /// <summary>Drop in acceptance rate vs learning profile expectations.</summary>
    public const string AcceptanceRateDrop = "AcceptanceRateDrop";
}
