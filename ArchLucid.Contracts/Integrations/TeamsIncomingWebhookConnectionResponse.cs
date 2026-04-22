namespace ArchLucid.Contracts.Integrations;

/// <summary>Read model for a tenant's Teams incoming-webhook configuration (secret values are never returned).</summary>
public sealed class TeamsIncomingWebhookConnectionResponse
{
    public required Guid TenantId { get; init; }

    /// <summary>True when a row exists for the tenant.</summary>
    public bool IsConfigured { get; init; }

    /// <summary>Optional operator label (e.g. channel name).</summary>
    public string? Label { get; init; }

    /// <summary>Name of the Key Vault secret the runtime resolves to obtain the webhook URL (never the URL itself).</summary>
    public string? KeyVaultSecretName { get; init; }

    /// <summary>
    /// Per-trigger opt-in matrix \u2014 always non-null. For an unconfigured row this is the v1 default (all-on)
    /// from <c>TeamsNotificationTriggerCatalog.All</c>; for a configured row this is the persisted subset, ordered
    /// canonically and filtered to known catalog entries.
    /// </summary>
    public IReadOnlyList<string> EnabledTriggers { get; init; } = [];

    public DateTimeOffset UpdatedUtc { get; init; }
}
