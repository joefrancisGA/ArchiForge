namespace ArchLucid.Decisioning.Alerts.Composite;

/// <summary>
///     Evaluates composite alert rules (aggregated metrics) with governance filtering and suppression before persistence.
/// </summary>
/// <remarks>
///     Implemented by <c>ArchLucid.Persistence.Alerts.CompositeAlertService</c>. Typically invoked in parallel with
///     <see cref="IAlertService" /> after building an <see cref="AlertEvaluationContext" />.
/// </remarks>
public interface ICompositeAlertService
{
    /// <summary>
    ///     Runs composite rules for the scope, filters by effective policy, and creates alerts for matches that pass
    ///     suppression.
    /// </summary>
    /// <param name="context">Shared evaluation context (same as simple alerts).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<CompositeAlertEvaluationResult> EvaluateAndPersistAsync(
        AlertEvaluationContext context,
        CancellationToken ct);
}
