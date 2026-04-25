namespace ArchLucid.Core.Billing;

/// <summary>Hosted checkout handoff (Stripe Checkout or Azure Marketplace landing).</summary>
public sealed class BillingCheckoutResult
{
    public required string CheckoutUrl
    {
        get;
        init;
    }

    public required string ProviderSessionId
    {
        get;
        init;
    }

    public DateTimeOffset? ExpiresUtc
    {
        get;
        init;
    }
}
