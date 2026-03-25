using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Workflow;

namespace ArchiForge.Decisioning.Alerts.Composite;

/// <summary>
/// Default <see cref="IAlertMetricSnapshotBuilder"/>: derives six metrics from plan, comparison, recommendations, and learning profile.
/// </summary>
/// <remarks>
/// Registered in DI for <c>ArchiForge.Persistence.Alerts.CompositeAlertService</c> (via interface). Threshold semantics for each metric are defined on stored <see cref="CompositeAlertRule"/> conditions.
/// </remarks>
public sealed class AlertMetricSnapshotBuilder : IAlertMetricSnapshotBuilder
{
    /// <inheritdoc />
    public AlertMetricSnapshot Build(AlertEvaluationContext context)
    {
        AlertMetricSnapshot snapshot = new AlertMetricSnapshot
        {
            CriticalRecommendationCount =
                context.ImprovementPlan?.Recommendations.Count(x =>
                    string.Equals(x.Urgency, "Critical", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Urgency, "High", StringComparison.OrdinalIgnoreCase)) ?? 0,

            NewComplianceGapCount =
                context.ComparisonResult?.SecurityChanges.Count ?? 0,

            DeferredHighPriorityRecommendationCount =
                context.RecommendationRecords.Count(x =>
                    string.Equals(x.Status, RecommendationStatus.Deferred, StringComparison.OrdinalIgnoreCase) &&
                    x.PriorityScore >= 80),

            RejectedSecurityRecommendationCount =
                context.RecommendationRecords.Count(x =>
                    string.Equals(x.Status, RecommendationStatus.Rejected, StringComparison.OrdinalIgnoreCase) &&
                    x.Category.Equals("Security", StringComparison.OrdinalIgnoreCase)),

            AcceptanceRatePercent = BuildAcceptanceRatePercent(context),
            CostIncreasePercent = BuildCostIncreasePercent(context)
        };

        return snapshot;
    }

    /// <summary>First cost delta on the comparison result; 0 when baseline cost is missing or zero.</summary>
    private static decimal BuildCostIncreasePercent(AlertEvaluationContext context)
    {
        CostDelta? delta = context.ComparisonResult?.CostChanges.FirstOrDefault();
        if (delta?.BaseCost is null || delta.TargetCost is null || delta.BaseCost == 0)
            return 0;

        return (delta.TargetCost.Value - delta.BaseCost.Value) / delta.BaseCost.Value * 100m;
    }

    /// <summary>Sum of accepted ÷ sum of proposed across learning categories.</summary>
    private static decimal BuildAcceptanceRatePercent(AlertEvaluationContext context)
    {
        RecommendationLearningProfile? profile = context.LearningProfile;
        if (profile is null)
            return 0;

        int proposed = profile.CategoryStats.Sum(x => x.ProposedCount);
        if (proposed == 0)
            return 0;

        int accepted = profile.CategoryStats.Sum(x => x.AcceptedCount);
        return (decimal)accepted / proposed * 100m;
    }
}
