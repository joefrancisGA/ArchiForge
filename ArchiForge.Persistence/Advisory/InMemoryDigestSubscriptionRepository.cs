using ArchiForge.Decisioning.Advisory.Delivery;

namespace ArchiForge.Persistence.Advisory;

public sealed class InMemoryDigestSubscriptionRepository : IDigestSubscriptionRepository
{
    private readonly List<DigestSubscription> _items = [];
    private readonly Lock _gate = new();

    public Task CreateAsync(DigestSubscription subscription, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _items.Add(subscription);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DigestSubscription subscription, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var i = _items.FindIndex(x => x.SubscriptionId == subscription.SubscriptionId);
            if (i >= 0)
                _items[i] = subscription;
        }

        return Task.CompletedTask;
    }

    public Task<DigestSubscription?> GetByIdAsync(Guid subscriptionId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            return Task.FromResult(_items.FirstOrDefault(x => x.SubscriptionId == subscriptionId));
    }

    public Task<IReadOnlyList<DigestSubscription>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId)
                .OrderByDescending(x => x.CreatedUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<DigestSubscription>>(result);
        }
    }

    public Task<IReadOnlyList<DigestSubscription>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.WorkspaceId == workspaceId &&
                    x.ProjectId == projectId &&
                    x.IsEnabled)
                .OrderByDescending(x => x.CreatedUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<DigestSubscription>>(result);
        }
    }
}
