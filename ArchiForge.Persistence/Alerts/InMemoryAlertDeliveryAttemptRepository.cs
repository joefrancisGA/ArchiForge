using ArchiForge.Decisioning.Alerts.Delivery;

namespace ArchiForge.Persistence.Alerts;

/// <summary>In-memory <see cref="IAlertDeliveryAttemptRepository"/> for tests; thread-safe via lock.</summary>
public sealed class InMemoryAlertDeliveryAttemptRepository : IAlertDeliveryAttemptRepository
{
    private readonly List<AlertDeliveryAttempt> _items = [];
    private readonly object _gate = new();

    public Task CreateAsync(AlertDeliveryAttempt attempt, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _items.Add(attempt);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AlertDeliveryAttempt attempt, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var i = _items.FindIndex(x => x.AlertDeliveryAttemptId == attempt.AlertDeliveryAttemptId);
            if (i >= 0)
                _items[i] = attempt;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AlertDeliveryAttempt>> ListByAlertAsync(Guid alertId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items.Where(x => x.AlertId == alertId).OrderByDescending(x => x.AttemptedUtc).ToList();
            return Task.FromResult<IReadOnlyList<AlertDeliveryAttempt>>(result);
        }
    }

    public Task<IReadOnlyList<AlertDeliveryAttempt>> ListBySubscriptionAsync(
        Guid routingSubscriptionId,
        int take,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x => x.RoutingSubscriptionId == routingSubscriptionId)
                .OrderByDescending(x => x.AttemptedUtc)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<AlertDeliveryAttempt>>(result);
        }
    }
}
