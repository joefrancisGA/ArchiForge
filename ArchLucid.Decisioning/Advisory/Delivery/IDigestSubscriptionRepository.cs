namespace ArchiForge.Decisioning.Advisory.Delivery;

/// <summary>
/// CRUD and scoped listing for <see cref="DigestSubscription"/> (where architecture digests are sent after scans).
/// </summary>
/// <remarks>
/// SQL: <c>DapperDigestSubscriptionRepository</c> on <c>dbo.DigestSubscriptions</c>. Consumed by <c>DigestDeliveryDispatcher</c> and <c>DigestSubscriptionsController</c>.
/// </remarks>
public interface IDigestSubscriptionRepository
{
    /// <summary>Inserts a new subscription row.</summary>
    Task CreateAsync(DigestSubscription subscription, CancellationToken ct);

    /// <summary>Updates mutable fields including <see cref="DigestSubscription.LastDeliveredUtc"/> after successful delivery.</summary>
    Task UpdateAsync(DigestSubscription subscription, CancellationToken ct);

    /// <summary>Loads by id (scope checks often done at API layer).</summary>
    Task<DigestSubscription?> GetByIdAsync(Guid subscriptionId, CancellationToken ct);

    /// <summary>All subscriptions in scope.</summary>
    Task<IReadOnlyList<DigestSubscription>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    /// <summary>Enabled subscriptions only; used when dispatching a digest.</summary>
    Task<IReadOnlyList<DigestSubscription>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}
