namespace ArchLucid.Decisioning.Alerts;

/// <summary>
///     Pure evaluation of simple (non-composite) alert rules against a populated <see cref="AlertEvaluationContext" />.
/// </summary>
public interface IAlertEvaluator
{
    /// <summary>
    ///     Runs all enabled rules in order; each rule may append at most one generated alert to the result list
    ///     (implementation-defined per rule type).
    /// </summary>
    /// <param name="rules">Typically already filtered by scope and governance.</param>
    /// <param name="context">Plan, comparison, recommendations, and optional preloaded governance.</param>
    /// <returns>New alert DTOs (not yet persisted).</returns>
    IReadOnlyList<AlertRecord> Evaluate(
        IReadOnlyList<AlertRule> rules,
        AlertEvaluationContext context);
}
