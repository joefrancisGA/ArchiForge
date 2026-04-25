namespace ArchLucid.Decisioning.Alerts;

/// <summary>
///     Evaluates configured alert rules for a run or scan, persists new alerts, and supports lifecycle actions
///     (ack/resolve/suppress).
/// </summary>
/// <remarks>
///     Implemented by <c>ArchLucid.Persistence.Alerts.AlertService</c>. Consumed from HTTP (alerts API), authority
///     workflows, and advisory scan runners.
/// </remarks>
public interface IAlertService
{
    /// <summary>
    ///     Loads rules for the context’s scope, applies governance filtering, evaluates, persists unseen dedup keys, and
    ///     dispatches delivery.
    /// </summary>
    /// <param name="context">
    ///     Tenant/workspace/project, run ids, and optional preloaded
    ///     <see cref="AlertEvaluationContext.EffectiveGovernanceContent" />.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task<AlertEvaluationOutcome> EvaluateAndPersistAsync(
        AlertEvaluationContext context,
        CancellationToken ct);

    /// <summary>Updates alert status from an operator action and writes audit when the status changes.</summary>
    /// <param name="alertId">Target alert.</param>
    /// <param name="userId">Acting user id.</param>
    /// <param name="userName">Acting user display name.</param>
    /// <param name="request">Action and optional comment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated record, or <c>null</c> if not found.</returns>
    Task<AlertRecord?> ApplyActionAsync(
        Guid alertId,
        string userId,
        string userName,
        AlertActionRequest request,
        CancellationToken ct);
}
