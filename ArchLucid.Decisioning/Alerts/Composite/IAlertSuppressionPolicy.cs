namespace ArchLucid.Decisioning.Alerts.Composite;

/// <summary>
///     After a composite rule matches, decides whether to create a new <see cref="AlertRecord" /> based on deduplication,
///     cooldown, and suppression windows.
/// </summary>
/// <remarks>
///     Default implementation: <c>ArchLucid.Persistence.Alerts.AlertSuppressionPolicy</c>.
/// </remarks>
public interface IAlertSuppressionPolicy
{
    /// <summary>
    ///     Inspects existing open/acknowledged alerts for the computed deduplication key (see
    ///     <see cref="CompositeAlertRule.DedupeScope" />).
    /// </summary>
    /// <param name="rule">Rule defining cooldown and suppression minutes.</param>
    /// <param name="context">Supplies scope and run ids for key materialization.</param>
    /// <param name="snapshot">Reserved for future signal-based suppression; currently unused.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AlertSuppressionDecision> DecideAsync(
        CompositeAlertRule rule,
        AlertEvaluationContext context,
        AlertMetricSnapshot snapshot,
        CancellationToken ct);
}
