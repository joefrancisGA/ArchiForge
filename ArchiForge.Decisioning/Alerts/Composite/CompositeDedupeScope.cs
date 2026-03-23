namespace ArchiForge.Decisioning.Alerts.Composite;

/// <summary>
/// Values for <see cref="CompositeAlertRule.DedupeScope"/>; control how <see cref="IAlertSuppressionPolicy"/> builds deduplication keys.
/// </summary>
public static class CompositeDedupeScope
{
    /// <summary>Key is stable per composite rule id only.</summary>
    public const string RuleOnly = "RuleOnly";

    /// <summary>Key includes current <see cref="AlertEvaluationContext.RunId"/>.</summary>
    public const string RuleAndRun = "RuleAndRun";

    /// <summary>Key includes run and comparison run ids.</summary>
    public const string RuleAndComparison = "RuleAndComparison";
}
