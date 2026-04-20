namespace ArchLucid.Contracts.Notifications;

/// <summary>Body for <c>PUT /v1/notifications/customer-channel-preferences</c> (Execute+; tenant from scope).</summary>
public sealed class TenantNotificationChannelPreferencesUpsertRequest
{
    /// <summary>When true, Logic Apps may send governance promotion customer email (addresses still come from connectors / Key Vault).</summary>
    public bool EmailCustomerNotificationsEnabled { get; init; }

    /// <summary>When true, Teams branch may run for customer promotion notices.</summary>
    public bool TeamsCustomerNotificationsEnabled { get; init; }

    /// <summary>When true, signed outbound webhook branch may run.</summary>
    public bool OutboundWebhookCustomerNotificationsEnabled { get; init; }
}
