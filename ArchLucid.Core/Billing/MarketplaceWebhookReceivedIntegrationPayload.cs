using ArchLucid.Core.Configuration;
using ArchLucid.Core.Integration;

namespace ArchLucid.Core.Billing;

/// <summary>
///     Domain summary for <see cref="IntegrationEventTypes.BillingMarketplaceWebhookReceivedV1" /> (no raw JWT or
///     full webhook body).
/// </summary>
public sealed class MarketplaceWebhookReceivedIntegrationPayload
{
    public Guid TenantId
    {
        get;
        init;
    }

    public Guid WorkspaceId
    {
        get;
        init;
    }

    public Guid ProjectId
    {
        get;
        init;
    }

    /// <summary>Stable idempotency key aligned with <c>dbo.BillingWebhookEvents</c> dedupe semantics.</summary>
    public string ProviderDedupeKey
    {
        get;
        init;
    } = string.Empty;

    public string Action
    {
        get;
        init;
    } = string.Empty;

    public string SubscriptionId
    {
        get;
        init;
    } = string.Empty;

    public string BillingProvider
    {
        get;
        init;
    } = BillingProviderNames.AzureMarketplace;
}
