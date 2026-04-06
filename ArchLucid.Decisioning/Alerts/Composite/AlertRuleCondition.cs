namespace ArchiForge.Decisioning.Alerts.Composite;

/// <summary>
/// One predicate in a <see cref="CompositeAlertRule"/>: metric id, comparison operator, and threshold.
/// </summary>
/// <remarks>
/// <see cref="MetricType"/> should be a <see cref="AlertMetricType"/> constant; <see cref="Operator"/> uses <see cref="AlertConditionOperator"/> values.
/// </remarks>
public class AlertRuleCondition
{
    /// <summary>Primary key (per condition row).</summary>
    public Guid ConditionId { get; set; } = Guid.NewGuid();

    /// <summary>Which snapshot field to read (e.g. <see cref="AlertMetricType.CostIncreasePercent"/>).</summary>
    public string MetricType { get; set; } = null!;

    /// <summary>Comparison to apply against <see cref="ThresholdValue"/>.</summary>
    public string Operator { get; set; } = AlertConditionOperator.GreaterThanOrEqual;

    /// <summary>Right-hand side of the comparison.</summary>
    public decimal ThresholdValue { get; set; }
}
