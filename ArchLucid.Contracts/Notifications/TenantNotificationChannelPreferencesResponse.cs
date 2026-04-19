namespace ArchLucid.Contracts.Notifications;

/// <summary>Read model for <c>GET /v1/notifications/customer-channel-preferences</c> (Logic Apps / governance promotion customer fan-out).</summary>
public sealed class TenantNotificationChannelPreferencesResponse
{
    /// <summary>Contract version; increment when adding non-optional fields.</summary>
    public int SchemaVersion { get; init; } = 1;

    public Guid TenantId { get; init; }

    /// <summary>When false, the response is synthesized defaults (no row in <c>dbo.TenantNotificationChannelPreferences</c>).</summary>
    public bool IsConfigured { get; init; }

    /// <summary>When true, Logic Apps may send governance promotion customer email (addresses still come from Key Vault / connector config, not this API).</summary>
    public bool EmailCustomerNotificationsEnabled { get; init; }

    /// <summary>When true, Teams branch may run (connector URLs outside SQL).</summary>
    public bool TeamsCustomerNotificationsEnabled { get; init; }

    /// <summary>When true, signed outbound webhook branch may run.</summary>
    public bool OutboundWebhookCustomerNotificationsEnabled { get; init; }

    public DateTimeOffset UpdatedUtc { get; init; }

    /// <summary>Defaults for tenants with no persisted row (conservative except email on).</summary>
    public static TenantNotificationChannelPreferencesResponse Unconfigured(Guid tenantId)
    {
        return new TenantNotificationChannelPreferencesResponse
        {
            SchemaVersion = 1,
            TenantId = tenantId,
            IsConfigured = false,
            EmailCustomerNotificationsEnabled = true,
            TeamsCustomerNotificationsEnabled = false,
            OutboundWebhookCustomerNotificationsEnabled = false,
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
    }
}
