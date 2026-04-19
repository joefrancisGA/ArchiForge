using ArchLucid.Core.Integration;

namespace ArchLucid.Core.Billing;

/// <summary>Outcome of provider webhook handling (HTTP layer maps to status codes).</summary>
public sealed class BillingWebhookHandleResult
{
    public bool Succeeded { get; init; }

    /// <summary>When true, caller should return 200 without re-applying side effects (duplicate provider event id).</summary>
    public bool DuplicateIgnored { get; init; }

    public string? ErrorDetail { get; init; }

    /// <summary>When set, HTTP host may publish <see cref="IntegrationEventTypes.BillingMarketplaceWebhookReceivedV1"/> after a successful 200.</summary>
    public MarketplaceWebhookReceivedIntegrationPayload? MarketplaceWebhookReceived { get; init; }

    public static BillingWebhookHandleResult Ok() => new() { Succeeded = true };

    public static BillingWebhookHandleResult Ok(MarketplaceWebhookReceivedIntegrationPayload marketplaceWebhookReceived)
    {
        ArgumentNullException.ThrowIfNull(marketplaceWebhookReceived);

        return new BillingWebhookHandleResult
        {
            Succeeded = true,
            MarketplaceWebhookReceived = marketplaceWebhookReceived,
        };
    }

    public static BillingWebhookHandleResult Duplicate() => new() { Succeeded = true, DuplicateIgnored = true };

    public static BillingWebhookHandleResult Rejected(string detail) => new() { ErrorDetail = detail };
}
