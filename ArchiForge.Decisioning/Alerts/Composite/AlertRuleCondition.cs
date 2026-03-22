namespace ArchiForge.Decisioning.Alerts.Composite;

public class AlertRuleCondition
{
    public Guid ConditionId { get; set; } = Guid.NewGuid();

    public string MetricType { get; set; } = null!;
    public string Operator { get; set; } = AlertConditionOperator.GreaterThanOrEqual;
    public decimal ThresholdValue { get; set; }
}
