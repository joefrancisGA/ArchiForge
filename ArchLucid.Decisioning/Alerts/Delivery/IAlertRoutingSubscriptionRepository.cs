namespace ArchLucid.Decisioning.Alerts.Delivery;

/// <summary>
///     CRUD and scoped queries for <see cref="AlertRoutingSubscription" /> (where alerts are delivered per
///     tenant/workspace/project).
/// </summary>
/// <remarks>
///     SQL: <c>DapperAlertRoutingSubscriptionRepository</c> on <c>dbo.AlertRoutingSubscriptions</c>. Consumed by
///     <c>AlertDeliveryDispatcher</c> and <c>AlertRoutingSubscriptionsController</c>.
/// </remarks>
public interface IAlertRoutingSubscriptionRepository
{
    /// <summary>Inserts a new subscription row.</summary>
    Task CreateAsync(AlertRoutingSubscription subscription, CancellationToken ct);

    /// <summary>
    ///     Updates mutable fields (including <see cref="AlertRoutingSubscription.LastDeliveredUtc" /> after successful
    ///     delivery).
    /// </summary>
    Task UpdateAsync(AlertRoutingSubscription subscription, CancellationToken ct);

    /// <summary>Loads by id (scope check left to callers when needed).</summary>
    Task<AlertRoutingSubscription?> GetByIdAsync(Guid routingSubscriptionId, CancellationToken ct);

    /// <summary>All subscriptions in scope, newest first.</summary>
    Task<IReadOnlyList<AlertRoutingSubscription>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    /// <summary>Enabled subscriptions only; used when dispatching alerts.</summary>
    Task<IReadOnlyList<AlertRoutingSubscription>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}
