using ArchLucid.Contracts.Notifications;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>Persistence for <c>dbo.TenantExecDigestPreferences</c>.</summary>
public interface ITenantExecDigestPreferencesRepository
{
    Task<ExecDigestPreferencesResponse?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<ExecDigestPreferencesResponse?> UpsertAsync(
        Guid tenantId,
        bool emailEnabled,
        IReadOnlyList<string> recipientEmails,
        string ianaTimeZoneId,
        int dayOfWeek,
        int hourOfDay,
        CancellationToken cancellationToken);

    /// <summary>Returns tenant ids where <c>EmailEnabled = 1</c>.</summary>
    Task<IReadOnlyList<Guid>> ListEmailEnabledTenantIdsAsync(CancellationToken cancellationToken);

    Task<bool> TryDisableEmailAsync(Guid tenantId, CancellationToken cancellationToken);
}
