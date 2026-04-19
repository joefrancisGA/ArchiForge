using ArchLucid.Contracts.Notifications;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>In-memory host: no SQL row store — always returns null so API serves <see cref="TenantNotificationChannelPreferencesResponse.Unconfigured"/>.</summary>
public sealed class InMemoryTenantNotificationChannelPreferencesRepository : ITenantNotificationChannelPreferencesRepository
{
    /// <inheritdoc />
    public Task<TenantNotificationChannelPreferencesResponse?> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<TenantNotificationChannelPreferencesResponse?>(null);
    }
}
