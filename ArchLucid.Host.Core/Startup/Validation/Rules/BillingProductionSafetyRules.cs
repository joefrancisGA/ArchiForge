using ArchLucid.Core.Configuration;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

/// <summary>
/// Production-only billing / Marketplace guards (Stripe live + Marketplace landing + GA offer binding).
/// Lives in <c>ArchLucid.Host.Core</c> with <see cref="ProductionSafetyRules"/> — not Host.Composition — so
/// <see cref="ArchLucidConfigurationRules"/> can call it without a circular project reference.
/// </summary>
internal static class BillingProductionSafetyRules
{
    /// <summary>Stripe <c>sk_live_*</c> without a webhook signing secret is unsafe in Production (unsigned events).</summary>
    public static void CollectStripeLiveKeyRequiresWebhookSigningSecret(IConfiguration configuration, List<string> errors)
    {
        BillingOptions billing =
            configuration.GetSection(BillingOptions.SectionName).Get<BillingOptions>() ?? new BillingOptions();

        string? secretKey = billing.Stripe.SecretKey?.Trim();

        if (string.IsNullOrWhiteSpace(secretKey))
            return;

        if (!secretKey.StartsWith("sk_live_", StringComparison.Ordinal))
            return;

        if (!string.IsNullOrWhiteSpace(billing.Stripe.WebhookSigningSecret?.Trim()))
            return;

        errors.Add(
            "Billing:Stripe:SecretKey uses live Stripe prefix sk_live_; configure Billing:Stripe:WebhookSigningSecret in Production so webhook signatures can be verified.");
    }

    /// <summary>Azure Marketplace checkout requires a public HTTPS landing URL (no loopback hosts).</summary>
    public static void CollectAzureMarketplaceLandingPageUrl(IConfiguration configuration, List<string> errors)
    {
        BillingOptions billing =
            configuration.GetSection(BillingOptions.SectionName).Get<BillingOptions>() ?? new BillingOptions();

        if (!string.Equals(billing.Provider.Trim(), BillingProviderNames.AzureMarketplace, StringComparison.OrdinalIgnoreCase))
            return;

        string? landing = billing.AzureMarketplace.LandingPageUrl?.Trim();

        if (string.IsNullOrWhiteSpace(landing))
        {
            errors.Add(
                "Billing:Provider is AzureMarketplace; configure Billing:AzureMarketplace:LandingPageUrl with an absolute HTTPS URL (Partner Center landing page).");

            return;
        }

        if (!Uri.TryCreate(landing, UriKind.Absolute, out Uri? uri))
        {
            errors.Add("Billing:AzureMarketplace:LandingPageUrl must be an absolute URI in Production.");

            return;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add("Billing:AzureMarketplace:LandingPageUrl must use http or https in Production.");

            return;
        }

        if (IsLocalOrLoopbackHost(uri.Host))

            errors.Add(
                "Billing:AzureMarketplace:LandingPageUrl must not use a localhost / loopback host in Production (Partner Center cannot reach it).");
    }

    /// <summary>GA Marketplace mutations require a configured Partner Center offer id.</summary>
    public static void CollectAzureMarketplaceGaRequiresOfferId(IConfiguration configuration, List<string> errors)
    {
        BillingOptions billing =
            configuration.GetSection(BillingOptions.SectionName).Get<BillingOptions>() ?? new BillingOptions();

        if (!string.Equals(billing.Provider.Trim(), BillingProviderNames.AzureMarketplace, StringComparison.OrdinalIgnoreCase))
            return;

        if (!billing.AzureMarketplace.GaEnabled)
            return;

        if (!string.IsNullOrWhiteSpace(billing.AzureMarketplace.MarketplaceOfferId?.Trim()))
            return;

        errors.Add(
            "Billing:AzureMarketplace:GaEnabled=true requires Billing:AzureMarketplace:MarketplaceOfferId (Partner Center transactable offer / product id) in Production.");
    }

    private static bool IsLocalOrLoopbackHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return true;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        return host is "127.0.0.1" or "::1" || host.StartsWith("127.", StringComparison.Ordinal);
    }
}
