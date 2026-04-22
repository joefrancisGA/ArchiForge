namespace ArchLucid.Contracts.Integrations;

/// <summary>Upsert body for <c>POST /v1/integrations/teams/connections</c> — stores a Key Vault secret *name* reference only.</summary>
public sealed class TeamsIncomingWebhookConnectionUpsertRequest
{
    /// <summary>Key Vault secret name (or fully qualified secret id without the secret *value*) used by Logic Apps / workers to resolve the Teams incoming webhook URL.</summary>
    public required string KeyVaultSecretName { get; init; }

    public string? Label { get; init; }

    /// <summary>
    /// Per-trigger opt-in subset of <c>TeamsNotificationTriggerCatalog.All</c>. <c>null</c> = leave the existing
    /// stored value unchanged (or all-on for a fresh row). Empty list = explicitly opt out of every trigger.
    /// Unknown trigger names cause an HTTP 400 with the unknown names listed.
    /// </summary>
    public IReadOnlyList<string>? EnabledTriggers { get; init; }
}
