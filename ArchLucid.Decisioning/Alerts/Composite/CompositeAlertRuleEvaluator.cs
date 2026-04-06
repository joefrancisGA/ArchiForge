namespace ArchiForge.Decisioning.Alerts.Composite;

/// <summary>
/// Stateless evaluator: maps each <see cref="AlertRuleCondition"/> to a boolean via <see cref="AlertConditionOperator"/>, then reduces with <see cref="CompositeOperator"/>.
/// </summary>
/// <remarks>
/// Unknown <see cref="AlertMetricType"/> values resolve to metric <c>0</c> in <see cref="ResolveMetric"/>.
/// </remarks>
public sealed class CompositeAlertRuleEvaluator : ICompositeAlertRuleEvaluator
{
    /// <inheritdoc />
    public bool Evaluate(CompositeAlertRule rule, AlertMetricSnapshot snapshot)
    {
        if (rule.Conditions.Count == 0)
            return false;

        List<bool> results = rule.Conditions.Select(condition =>
            EvaluateCondition(condition, snapshot)).ToList();

        return rule.Operator switch
        {
            CompositeOperator.And => results.All(x => x),
            CompositeOperator.Or => results.Any(x => x),
            _ => false,
        };
    }

    /// <summary>Compares resolved metric to <see cref="AlertRuleCondition.ThresholdValue"/> using the condition’s operator.</summary>
    private static bool EvaluateCondition(AlertRuleCondition condition, AlertMetricSnapshot snapshot)
    {
        decimal actual = ResolveMetric(condition.MetricType, snapshot);
        decimal expected = condition.ThresholdValue;

        return condition.Operator switch
        {
            AlertConditionOperator.GreaterThanOrEqual => actual >= expected,
            AlertConditionOperator.GreaterThan => actual > expected,
            AlertConditionOperator.LessThanOrEqual => actual <= expected,
            AlertConditionOperator.LessThan => actual < expected,
            AlertConditionOperator.Equal => actual == expected,
            AlertConditionOperator.NotEqual => actual != expected,
            _ => false,
        };
    }

    /// <summary>Maps string metric type constants (<see cref="AlertMetricType"/>) to snapshot fields.</summary>
    private static decimal ResolveMetric(string metricType, AlertMetricSnapshot snapshot)
    {
        return metricType switch
        {
            AlertMetricType.CriticalRecommendationCount => snapshot.CriticalRecommendationCount,
            AlertMetricType.NewComplianceGapCount => snapshot.NewComplianceGapCount,
            AlertMetricType.CostIncreasePercent => snapshot.CostIncreasePercent,
            AlertMetricType.DeferredHighPriorityRecommendationCount => snapshot.DeferredHighPriorityRecommendationCount,
            AlertMetricType.RejectedSecurityRecommendationCount => snapshot.RejectedSecurityRecommendationCount,
            AlertMetricType.AcceptanceRatePercent => snapshot.AcceptanceRatePercent,
            _ => 0,
        };
    }
}
