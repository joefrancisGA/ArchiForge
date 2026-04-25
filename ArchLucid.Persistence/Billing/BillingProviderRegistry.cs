using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;
using ArchLucid.Persistence.Billing.AzureMarketplace;
using ArchLucid.Persistence.Billing.Stripe;

using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Billing;

public sealed class BillingProviderRegistry(
    IOptionsMonitor<BillingOptions> billingOptions,
    NoopBillingProvider noop,
    StripeBillingProvider stripe,
    AzureMarketplaceBillingProvider azureMarketplace) : IBillingProviderRegistry
{
    private readonly AzureMarketplaceBillingProvider _azureMarketplace =
        azureMarketplace ?? throw new ArgumentNullException(nameof(azureMarketplace));

    private readonly IOptionsMonitor<BillingOptions> _billingOptions =
        billingOptions ?? throw new ArgumentNullException(nameof(billingOptions));

    private readonly NoopBillingProvider _noop = noop ?? throw new ArgumentNullException(nameof(noop));

    private readonly StripeBillingProvider _stripe = stripe ?? throw new ArgumentNullException(nameof(stripe));

    public IBillingProvider ResolveActiveProvider()
    {
        string name = _billingOptions.CurrentValue.Provider.Trim();

        if (string.Equals(name, BillingProviderNames.Stripe, StringComparison.OrdinalIgnoreCase))  return _stripe;

        if (string.Equals(name, BillingProviderNames.AzureMarketplace, StringComparison.OrdinalIgnoreCase))
            return _azureMarketplace;

        return _noop;
    }
}
