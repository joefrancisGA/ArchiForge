namespace ArchLucid.Core.Billing;

/// <summary>Raw HTTP payload for <see cref="IBillingProvider.HandleWebhookAsync" />.</summary>
public sealed class BillingWebhookInbound
{
    public required string RawBody
    {
        get;
        init;
    }

    /// <summary>Stripe <c>Stripe-Signature</c> header value.</summary>
    public string? StripeSignatureHeader
    {
        get;
        init;
    }

    /// <summary>Azure Marketplace SaaS webhook <c>Authorization: Bearer …</c> token (without the prefix).</summary>
    public string? MarketplaceAuthorizationBearer
    {
        get;
        init;
    }
}
