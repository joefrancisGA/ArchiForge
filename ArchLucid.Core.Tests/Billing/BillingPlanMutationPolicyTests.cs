using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class BillingPlanMutationPolicyTests
{
    [Fact]
    public void WebhookPlanMutationsEnabled_true_when_stripe_provider_even_if_marketplace_ga_off()
    {
        BillingOptions billing = new()
        {
            Provider = BillingProviderNames.Stripe,
            AzureMarketplace = new AzureMarketplaceBillingOptions { GaEnabled = false }
        };

        BillingPlanMutationPolicy.WebhookPlanMutationsEnabled(billing).Should().BeTrue();
    }

    [Fact]
    public void WebhookPlanMutationsEnabled_false_when_marketplace_provider_and_ga_off()
    {
        BillingOptions billing = new()
        {
            Provider = BillingProviderNames.AzureMarketplace,
            AzureMarketplace = new AzureMarketplaceBillingOptions { GaEnabled = false }
        };

        BillingPlanMutationPolicy.WebhookPlanMutationsEnabled(billing).Should().BeFalse();
    }
}
