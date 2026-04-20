using ArchLucid.Contracts.Notifications;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>Reads <c>dbo.TenantNotificationChannelPreferences</c> for Logic Apps governance promotion customer fan-out.</summary>
public interface ITenantNotificationChannelPreferencesRepository
{
    /// <summary>Returns null when no row exists for <paramref name="tenantId"/>.</summary>
    Task<TenantNotificationChannelPreferencesResponse?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts or updates <c>dbo.TenantNotificationChannelPreferences</c> for <paramref name="tenantId"/>.
    /// Returns null when <c>dbo.Tenants</c> has no row for <paramref name="tenantId"/> (FK would fail).
    /// </summary>
    Task<TenantNotificationChannelPreferencesResponse?> UpsertAsync(
        Guid tenantId,
        bool emailCustomerNotificationsEnabled,
        bool teamsCustomerNotificationsEnabled,
        bool outboundWebhookCustomerNotificationsEnabled,
        CancellationToken cancellationToken);
}
