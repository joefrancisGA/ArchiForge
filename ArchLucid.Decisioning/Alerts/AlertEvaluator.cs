using ArchLucid.Core.Comparison;
using ArchLucid.Decisioning.Advisory.Learning;
using ArchLucid.Decisioning.Advisory.Workflow;
using ArchLucid.Decisioning.Governance.PolicyPacks;

namespace ArchLucid.Decisioning.Alerts;

/// <summary>
///     Stateless evaluator: maps each enabled <see cref="AlertRule" /> to zero or one <see cref="AlertRecord" /> using
///     metrics from <see cref="AlertEvaluationContext" />.
/// </summary>
/// <remarks>
///     Invoked from <c>ArchLucid.Persistence.Alerts.AlertService</c> after rules are filtered by
///     <see cref="PolicyPackGovernanceFilter" />.
///     Does not persist or deduplicate; callers own repository and delivery.
/// </remarks>
public sealed class AlertEvaluator : IAlertEvaluator
{
    /// <inheritdoc />
    /// <remarks>
    ///     Only rules with <see cref="AlertRule.IsEnabled" /> are considered. Unknown <see cref="AlertRuleType" /> values
    ///     are skipped.
    /// </remarks>
    public IReadOnlyList<AlertRecord> Evaluate(
        IReadOnlyList<AlertRule> rules,
        AlertEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(context);
        List<AlertRecord> alerts = [];

        foreach (AlertRule rule in rules.Where(x => x.IsEnabled))

            switch (rule.RuleType)
            {
                case AlertRuleType.CriticalRecommendationCount:
                    EvaluateCriticalRecommendationCount(rule, context, alerts);
                    break;

                case AlertRuleType.NewComplianceGapCount:
                    EvaluateNewComplianceGapCount(rule, context, alerts);
                    break;

                case AlertRuleType.CostIncreasePercent:
                    EvaluateCostIncreasePercent(rule, context, alerts);
                    break;

                case AlertRuleType.DeferredHighPriorityRecommendationAgeDays:
                    EvaluateDeferredHighPriorityAge(rule, context, alerts);
                    break;

                case AlertRuleType.RejectedSecurityRecommendation:
                    EvaluateRejectedSecurityRecommendation(rule, context, alerts);
                    break;

                case AlertRuleType.AcceptanceRateDrop:
                    EvaluateAcceptanceRateDrop(rule, context, alerts);
                    break;
            }

        return alerts;
    }

    private static void EvaluateCriticalRecommendationCount(
        AlertRule rule,
        AlertEvaluationContext context,
        List<AlertRecord> alerts)
    {
        int count = context.ImprovementPlan?.Recommendations.Count(x =>
            string.Equals(x.Urgency, AlertUrgencies.Critical, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Urgency, AlertUrgencies.High, StringComparison.OrdinalIgnoreCase)) ?? 0;

        if (count >= rule.ThresholdValue)

            alerts.Add(BuildAlert(
                rule,
                context,
                "High number of critical/high-priority recommendations detected",
                AlertCategories.Advisory,
                count.ToString(),
                $"The current improvement plan contains {count} critical or high-priority recommendations.",
                null,
                $"critical-rec-count:{count}"));
    }

    private static void EvaluateNewComplianceGapCount(
        AlertRule rule,
        AlertEvaluationContext context,
        List<AlertRecord> alerts)
    {
        int count = context.ComparisonResult?.SecurityChanges.Count ?? 0;

        if (count >= rule.ThresholdValue)

            alerts.Add(BuildAlert(
                rule,
                context,
                "New compliance or security delta threshold exceeded",
                AlertCategories.Compliance,
                count.ToString(),
                $"The latest comparison produced {count} relevant compliance/security deltas.",
                null,
                $"comp-gap-count:{count}"));
    }

    private static void EvaluateCostIncreasePercent(
        AlertRule rule,
        AlertEvaluationContext context,
        List<AlertRecord> alerts)
    {
        CostDelta? delta = context.ComparisonResult?.CostChanges.FirstOrDefault();

        if (delta?.BaseCost is null || delta.TargetCost is null || delta.BaseCost == 0)
            return;

        decimal increasePct = (delta.TargetCost.Value - delta.BaseCost.Value) / delta.BaseCost.Value * 100m;

        if (increasePct >= rule.ThresholdValue)

            alerts.Add(BuildAlert(
                rule,
                context,
                "Projected cost increase exceeded threshold",
                AlertCategories.Cost,
                $"{increasePct:0.##}%",
                $"Projected cost increased by {increasePct:0.##}% compared to the baseline run.",
                null,
                $"cost-increase:{Math.Round(increasePct, 0)}"));
    }

    private static void EvaluateDeferredHighPriorityAge(
        AlertRule rule,
        AlertEvaluationContext context,
        List<AlertRecord> alerts)
    {
        DateTime cutoff = DateTime.UtcNow.AddDays(-(double)rule.ThresholdValue);

        alerts.AddRange(
            context.RecommendationRecords
                .Where(x =>
                    string.Equals(x.Status, RecommendationStatus.Deferred, StringComparison.OrdinalIgnoreCase) &&
                    x.PriorityScore >= 80 &&
                    x.LastUpdatedUtc <= cutoff)
                .Select(item => BuildAlert(
                    rule,
                    context,
                    "Deferred high-priority recommendation is aging",
                    AlertCategories.Recommendation,
                    item.LastUpdatedUtc.ToString("u"),
                    $"Recommendation '{item.Title}' has remained deferred beyond the configured threshold.",
                    item.RecommendationId,
                    $"deferred-aging:{item.RecommendationId}")));
    }

    private static void EvaluateRejectedSecurityRecommendation(
        AlertRule rule,
        AlertEvaluationContext context,
        List<AlertRecord> alerts)
    {
        alerts.AddRange(
            context.RecommendationRecords
                .Where(x =>
                    string.Equals(x.Status, RecommendationStatus.Rejected, StringComparison.OrdinalIgnoreCase) &&
                    x.Category.Equals(AlertCategories.Security, StringComparison.OrdinalIgnoreCase))
                .Select(item => BuildAlert(
                    rule,
                    context,
                    "Security recommendation was rejected",
                    AlertCategories.Security,
                    item.RecommendationId.ToString(),
                    $"Security recommendation '{item.Title}' was rejected.",
                    item.RecommendationId,
                    $"rejected-security:{item.RecommendationId}")));
    }

    private static void EvaluateAcceptanceRateDrop(
        AlertRule rule,
        AlertEvaluationContext context,
        List<AlertRecord> alerts)
    {
        RecommendationLearningProfile? profile = context.LearningProfile;

        if (profile is null)
            return;

        int proposed = profile.CategoryStats.Sum(x => x.ProposedCount);
        double overall = proposed == 0
            ? 0d
            : profile.CategoryStats.Sum(x => x.AcceptedCount) / (double)proposed;

        double pct = overall * 100d;

        if (pct <= (double)rule.ThresholdValue)

            alerts.Add(BuildAlert(
                rule,
                context,
                "Recommendation acceptance rate is below threshold",
                AlertCategories.Learning,
                $"{pct:0.##}%",
                $"Overall recommendation acceptance rate is {pct:0.##}%, below the configured threshold.",
                null,
                $"accept-rate:{Math.Round(pct, 0)}"));
    }

    private static AlertRecord BuildAlert(
        AlertRule rule,
        AlertEvaluationContext context,
        string title,
        string category,
        string triggerValue,
        string description,
        Guid? recommendationId,
        string dedupeSuffix)
    {
        return new AlertRecord
        {
            AlertId = Guid.NewGuid(),
            RuleId = rule.RuleId,
            TenantId = context.TenantId,
            WorkspaceId = context.WorkspaceId,
            ProjectId = context.ProjectId,
            RunId = context.RunId,
            ComparedToRunId = context.ComparedToRunId,
            RecommendationId = recommendationId,
            Title = title,
            Category = category,
            Severity = rule.Severity,
            Status = AlertStatus.Open,
            TriggerValue = triggerValue,
            Description = description,
            CreatedUtc = DateTime.UtcNow,
            DeduplicationKey = $"{rule.RuleId}:{dedupeSuffix}"
        };
    }
}
