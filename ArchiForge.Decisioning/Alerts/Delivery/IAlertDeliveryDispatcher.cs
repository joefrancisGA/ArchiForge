namespace ArchiForge.Decisioning.Alerts.Delivery;

/// <summary>
/// Delivers a single <see cref="AlertRecord"/> to all matching routing subscriptions (email, Slack, etc.).
/// </summary>
/// <remarks>
/// Implemented by <c>ArchiForge.Persistence.Alerts.AlertDeliveryDispatcher</c>. Invoked after persistence from alert services.
/// </remarks>
public interface IAlertDeliveryDispatcher
{
    /// <summary>
    /// Resolves enabled subscriptions for the alert’s scope, filters by minimum severity, records attempts, invokes channels, and audits success/failure per subscription.
    /// </summary>
    /// <param name="alert">Newly created alert to fan out.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeliverAsync(AlertRecord alert, CancellationToken ct);
}
