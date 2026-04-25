namespace ArchLucid.Decisioning.Alerts.Composite;

/// <summary>
///     Comparison operators for <see cref="AlertRuleCondition.Operator" /> against <see cref="AlertMetricSnapshot" />
///     fields.
/// </summary>
public static class AlertConditionOperator
{
    public const string GreaterThanOrEqual = "GreaterThanOrEqual";
    public const string GreaterThan = "GreaterThan";
    public const string LessThanOrEqual = "LessThanOrEqual";
    public const string LessThan = "LessThan";
    public const string Equal = "Equal";
    public const string NotEqual = "NotEqual";
}
