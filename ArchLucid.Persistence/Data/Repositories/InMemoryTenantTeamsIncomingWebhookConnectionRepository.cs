using System.Collections.Concurrent;

using ArchLucid.Contracts.Integrations;
using ArchLucid.Core.Notifications.Teams;

namespace ArchLucid.Persistence.Data.Repositories;

public sealed class
    InMemoryTenantTeamsIncomingWebhookConnectionRepository : ITenantTeamsIncomingWebhookConnectionRepository
{
    private readonly ConcurrentDictionary<Guid, TeamsIncomingWebhookConnectionResponse> _store = new();

    public Task<TeamsIncomingWebhookConnectionResponse?> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (_store.TryGetValue(tenantId, out TeamsIncomingWebhookConnectionResponse? row))
            return Task.FromResult<TeamsIncomingWebhookConnectionResponse?>(row);

        return Task.FromResult<TeamsIncomingWebhookConnectionResponse?>(null);
    }

    public Task<TeamsIncomingWebhookConnectionResponse?> UpsertAsync(
        Guid tenantId,
        string keyVaultSecretName,
        string? label,
        IReadOnlyList<string>? enabledTriggers,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> nextTriggers = ResolveNextTriggers(tenantId, enabledTriggers);

        TeamsIncomingWebhookConnectionResponse row = new()
        {
            TenantId = tenantId,
            IsConfigured = true,
            Label = label,
            KeyVaultSecretName = keyVaultSecretName,
            EnabledTriggers = nextTriggers,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        _store[tenantId] = row;

        return Task.FromResult<TeamsIncomingWebhookConnectionResponse?>(row);
    }

    public Task<bool> DeleteAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.TryRemove(tenantId, out _));
    }

    // null = "no change" semantic, matching the Dapper repository's MERGE COALESCE behaviour:
    // brand-new rows fall back to the catalog default; existing rows keep what was already stored.
    private IReadOnlyList<string> ResolveNextTriggers(Guid tenantId, IReadOnlyList<string>? enabledTriggers)
    {
        if (enabledTriggers is not null)
            return TeamsNotificationTriggerCatalog
                .ParseOrDefault(TeamsNotificationTriggerCatalog.Serialize(enabledTriggers));

        if (_store.TryGetValue(tenantId, out TeamsIncomingWebhookConnectionResponse? existing))
            return existing.EnabledTriggers;

        return TeamsNotificationTriggerCatalog.All;
    }
}
