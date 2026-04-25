namespace ArchLucid.Decisioning.Alerts.Delivery;

/// <summary>
///     Persistence for <see cref="AlertDeliveryAttempt" /> rows (one row per subscription per dispatch try).
/// </summary>
/// <remarks>
///     SQL: <c>DapperAlertDeliveryAttemptRepository</c> on <c>dbo.AlertDeliveryAttempts</c>. Written by
///     <c>AlertDeliveryDispatcher</c>;
///     listed from the alert routing API for operator visibility.
/// </remarks>
public interface IAlertDeliveryAttemptRepository
{
    /// <summary>Creates a row in <see cref="AlertDeliveryAttemptStatus.Started" /> before the channel send.</summary>
    Task CreateAsync(AlertDeliveryAttempt attempt, CancellationToken ct);

    /// <summary>Updates status, error message, and retry count after send completes or fails.</summary>
    Task UpdateAsync(AlertDeliveryAttempt attempt, CancellationToken ct);

    /// <summary>Delivery history for a single alert, newest first.</summary>
    Task<IReadOnlyList<AlertDeliveryAttempt>> ListByAlertAsync(
        Guid alertId,
        CancellationToken ct);

    /// <summary>Recent attempts for a subscription (e.g. health / debugging).</summary>
    Task<IReadOnlyList<AlertDeliveryAttempt>> ListBySubscriptionAsync(
        Guid routingSubscriptionId,
        int take,
        CancellationToken ct);
}
