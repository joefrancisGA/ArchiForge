using ArchiForge.Decisioning.Alerts.Delivery;

namespace ArchiForge.Persistence.Alerts;

public sealed class InMemoryAlertRoutingSubscriptionRepository : IAlertRoutingSubscriptionRepository
{
    private readonly List<AlertRoutingSubscription> _items = [];
    private readonly Lock _gate = new();

    public Task CreateAsync(AlertRoutingSubscription subscription, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _items.Add(subscription);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AlertRoutingSubscription subscription, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var i = _items.FindIndex(x => x.RoutingSubscriptionId == subscription.RoutingSubscriptionId);
            if (i >= 0)
                _items[i] = subscription;
        }

        return Task.CompletedTask;
    }

    public Task<AlertRoutingSubscription?> GetByIdAsync(Guid routingSubscriptionId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            return Task.FromResult(_items.FirstOrDefault(x => x.RoutingSubscriptionId == routingSubscriptionId));
    }

    public Task<IReadOnlyList<AlertRoutingSubscription>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x => x.TenantId == tenantId && x.WorkspaceId == workspaceId && x.ProjectId == projectId)
                .OrderByDescending(x => x.CreatedUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<AlertRoutingSubscription>>(result);
        }
    }

    public Task<IReadOnlyList<AlertRoutingSubscription>> ListEnabledByScopeAsync(
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
            return Task.FromResult<IReadOnlyList<AlertRoutingSubscription>>(result);
        }
    }
}
