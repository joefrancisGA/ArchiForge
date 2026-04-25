using ArchLucid.Core.Configuration;

namespace ArchLucid.Core.Billing;

/// <summary>
///     Centralizes when external billing webhooks may call <see cref="IBillingLedger.ChangePlanAsync" /> /
///     related stored procedures (Azure Marketplace GA flag or Stripe-hosted checkout).
/// </summary>
public static class BillingPlanMutationPolicy
{
    /// <summary>
    ///     Stripe Checkout completion reuses <see cref="IMarketplaceChangePlanWebhookMutationHandler" /> with a synthetic
    ///     <c>planId</c> payload — allow plan mutations when <see cref="BillingOptions.Provider" /> is Stripe even if
    ///     <see cref="AzureMarketplaceBillingOptions.GaEnabled" /> is still false.
    /// </summary>
    public static bool WebhookPlanMutationsEnabled(BillingOptions billing)
    {
        return billing.AzureMarketplace.GaEnabled || string.Equals(billing.Provider, BillingProviderNames.Stripe,
            StringComparison.OrdinalIgnoreCase);
    }
}
