using System.Collections.Concurrent;

using ArchLucid.Contracts.Notifications;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     In-memory host: stores preferences per tenant so JWT integration tests can exercise GET/PUT without SQL.
/// </summary>
public sealed class
    InMemoryTenantNotificationChannelPreferencesRepository : ITenantNotificationChannelPreferencesRepository
{
    private readonly ConcurrentDictionary<Guid, TenantNotificationChannelPreferencesResponse?> _store = new();

    /// <inheritdoc />
    public Task<TenantNotificationChannelPreferencesResponse?> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (_store.TryGetValue(tenantId, out TenantNotificationChannelPreferencesResponse? row) && row is not null)
            return Task.FromResult<TenantNotificationChannelPreferencesResponse?>(row);


        return Task.FromResult<TenantNotificationChannelPreferencesResponse?>(null);
    }

    /// <inheritdoc />
    public Task<TenantNotificationChannelPreferencesResponse?> UpsertAsync(
        Guid tenantId,
        bool emailCustomerNotificationsEnabled,
        bool teamsCustomerNotificationsEnabled,
        bool outboundWebhookCustomerNotificationsEnabled,
        CancellationToken cancellationToken)
    {
        TenantNotificationChannelPreferencesResponse row = new()
        {
            SchemaVersion = 1,
            TenantId = tenantId,
            IsConfigured = true,
            EmailCustomerNotificationsEnabled = emailCustomerNotificationsEnabled,
            TeamsCustomerNotificationsEnabled = teamsCustomerNotificationsEnabled,
            OutboundWebhookCustomerNotificationsEnabled = outboundWebhookCustomerNotificationsEnabled,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        _store[tenantId] = row;

        return Task.FromResult<TenantNotificationChannelPreferencesResponse?>(row);
    }
}
