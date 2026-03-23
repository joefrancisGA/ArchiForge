namespace ArchiForge.Decisioning.Alerts.Composite;

/// <summary>
/// Evaluates whether a <see cref="CompositeAlertRule"/>’s conditions hold against a precomputed <see cref="AlertMetricSnapshot"/>.
/// </summary>
public interface ICompositeAlertRuleEvaluator
{
    /// <summary>
    /// Returns <c>false</c> when the rule has no conditions or uses an unknown <see cref="CompositeOperator"/>.
    /// </summary>
    /// <param name="rule">Enabled rule with <see cref="CompositeAlertRule.Operator"/> combining per-condition results.</param>
    /// <param name="snapshot">Metrics from <see cref="IAlertMetricSnapshotBuilder"/>.</param>
    bool Evaluate(CompositeAlertRule rule, AlertMetricSnapshot snapshot);
}
