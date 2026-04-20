namespace ArchLucid.Core.Configuration;

/// <summary><c>Billing:*</c> configuration (checkout + webhook providers).</summary>
public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    /// <summary><see cref="BillingProviderNames"/> value.</summary>
    public string Provider { get; init; } = BillingProviderNames.Stripe;

    public StripeBillingOptions Stripe { get; init; } = new();

    public AzureMarketplaceBillingOptions AzureMarketplace { get; init; } = new();
}

public sealed class StripeBillingOptions
{
    /// <summary>Stripe secret API key (Key Vault secret in production).</summary>
    public string? SecretKey { get; init; }

    /// <summary>Stripe webhook signing secret (whsec_…).</summary>
    public string? WebhookSigningSecret { get; init; }

    /// <summary>Optional publishable key for client-side Stripe.js (not used server-side today).</summary>
    public string? PublishableKey { get; init; }

    public string? PriceIdTeam { get; init; }

    public string? PriceIdPro { get; init; }

    public string? PriceIdEnterprise { get; init; }
}

public sealed class AzureMarketplaceBillingOptions
{
    /// <summary>Landing URL returned from checkout when Marketplace is the provider (buyer completes in Azure).</summary>
    public string? LandingPageUrl { get; init; }

    /// <summary>Optional tenant id claim name inside the marketplace JWT (defaults to standard SaaS claims).</summary>
    public string? TenantIdClaimType { get; init; }

    /// <summary>OIDC metadata URL used to validate webhook JWT signatures (Microsoft identity platform).</summary>
    public string? OpenIdMetadataAddress { get; init; }

    /// <summary>Expected JWT audiences (e.g. <c>https://marketplaceapi.microsoft.com</c>).</summary>
    public string[]? ValidAudiences { get; init; }

    /// <summary>When false, skips the outbound SaaS activate HTTP call (useful for integration tests).</summary>
    public bool FulfillmentApiEnabled { get; init; } = true;

    /// <summary>
    /// When false, Marketplace <c>ChangePlan</c> / <c>ChangeQuantity</c> webhooks are acknowledged with HTTP 202 and
    /// <c>AcknowledgedNoOp</c> in <c>BillingWebhookEvents</c> without calling <c>sp_Billing_ChangePlan</c> /
    /// <c>sp_Billing_ChangeQuantity</c>. Enable in production after plan/quantity mapping is validated.
    /// </summary>
    public bool GaEnabled { get; init; }
}
