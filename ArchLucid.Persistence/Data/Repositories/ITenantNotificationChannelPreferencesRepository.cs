using ArchLucid.Contracts.Notifications;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>Reads <c>dbo.TenantNotificationChannelPreferences</c> for Logic Apps governance promotion customer fan-out.</summary>
public interface ITenantNotificationChannelPreferencesRepository
{
    /// <summary>Returns null when no row exists for <paramref name="tenantId"/>.</summary>
    Task<TenantNotificationChannelPreferencesResponse?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}
