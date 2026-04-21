using System.Collections.Concurrent;

using ArchLucid.Contracts.Notifications;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>In-memory store for integration tests and <see cref="ArchLucid.Core.Configuration.StorageProviderNames.InMemory"/>.</summary>
public sealed class InMemoryTenantExecDigestPreferencesRepository : ITenantExecDigestPreferencesRepository
{
    private readonly ConcurrentDictionary<Guid, ExecDigestPreferencesResponse> _store = new();

    public Task<ExecDigestPreferencesResponse?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (_store.TryGetValue(tenantId, out ExecDigestPreferencesResponse? row))
            return Task.FromResult<ExecDigestPreferencesResponse?>(row);

        return Task.FromResult<ExecDigestPreferencesResponse?>(null);
    }

    public Task<ExecDigestPreferencesResponse?> UpsertAsync(
        Guid tenantId,
        bool emailEnabled,
        IReadOnlyList<string> recipientEmails,
        string ianaTimeZoneId,
        int dayOfWeek,
        int hourOfDay,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        ExecDigestPreferencesResponse row = new()
        {
            SchemaVersion = 1,
            TenantId = tenantId,
            IsConfigured = true,
            EmailEnabled = emailEnabled,
            RecipientEmails = recipientEmails is null ? [] : recipientEmails.Where(static e => !string.IsNullOrWhiteSpace(e)).Select(static e => e.Trim()).ToList(),
            IanaTimeZoneId = string.IsNullOrWhiteSpace(ianaTimeZoneId) ? "UTC" : ianaTimeZoneId.Trim(),
            DayOfWeek = dayOfWeek,
            HourOfDay = hourOfDay,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };

        _store[tenantId] = row;

        return Task.FromResult<ExecDigestPreferencesResponse?>(row);
    }

    public Task<IReadOnlyList<Guid>> ListEmailEnabledTenantIdsAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        List<Guid> ids = _store
            .Where(static p => p.Value.EmailEnabled)
            .Select(static p => p.Key)
            .ToList();

        return Task.FromResult<IReadOnlyList<Guid>>(ids);
    }

    public Task<bool> TryDisableEmailAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!_store.TryGetValue(tenantId, out ExecDigestPreferencesResponse? existing))
            return Task.FromResult(false);

        if (!existing.EmailEnabled)
            return Task.FromResult(false);

        ExecDigestPreferencesResponse updated = new()
        {
            SchemaVersion = existing.SchemaVersion,
            TenantId = existing.TenantId,
            IsConfigured = existing.IsConfigured,
            EmailEnabled = false,
            RecipientEmails = existing.RecipientEmails,
            IanaTimeZoneId = existing.IanaTimeZoneId,
            DayOfWeek = existing.DayOfWeek,
            HourOfDay = existing.HourOfDay,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };

        _store[tenantId] = updated;

        return Task.FromResult(true);
    }
}
