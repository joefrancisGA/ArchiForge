using ArchLucid.Contracts.Integrations;

namespace ArchLucid.Persistence.Data.Repositories;

public interface ITenantTeamsIncomingWebhookConnectionRepository
{
    Task<TeamsIncomingWebhookConnectionResponse?> GetAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    ///     Upsert the Teams Key Vault reference + per-trigger opt-in matrix.
    /// </summary>
    /// <param name="enabledTriggers">
    ///     Validated subset of <c>TeamsNotificationTriggerCatalog.All</c>, or <c>null</c> to leave the persisted
    ///     triggers unchanged (or fall back to the all-on default for a brand-new row). Empty list = explicit opt-out
    ///     of every trigger.
    /// </param>
    Task<TeamsIncomingWebhookConnectionResponse?> UpsertAsync(
        Guid tenantId,
        string keyVaultSecretName,
        string? label,
        IReadOnlyList<string>? enabledTriggers,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid tenantId, CancellationToken cancellationToken);
}
