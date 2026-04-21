using ArchLucid.Core.Configuration;
using ArchLucid.Host.Core.Startup.Validation.Rules;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

namespace ArchLucid.Host.Composition.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class BillingProductionSafetyRulesTests
{
    [Fact]
    public void CollectStripeLiveKeyRequiresWebhookSigningSecret_when_sk_live_without_whsec_adds_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["Billing:Stripe:SecretKey"] = "sk_live_unit_test_placeholder_not_a_real_key",
            ["Billing:Stripe:WebhookSigningSecret"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        List<string> errors = [];

        BillingProductionSafetyRules.CollectStripeLiveKeyRequiresWebhookSigningSecret(configuration, errors);

        errors.Should()
            .ContainSingle(static e => e.Contains("sk_live_", StringComparison.Ordinal)
                                      && e.Contains("WebhookSigningSecret", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectStripeLiveKeyRequiresWebhookSigningSecret_when_sk_live_with_whsec_is_clean()
    {
        Dictionary<string, string?> data = new()
        {
            ["Billing:Stripe:SecretKey"] = "sk_live_unit_test_placeholder_not_a_real_key",
            ["Billing:Stripe:WebhookSigningSecret"] = "whsec_unit_test_placeholder_not_a_real_secret",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        List<string> errors = [];

        BillingProductionSafetyRules.CollectStripeLiveKeyRequiresWebhookSigningSecret(configuration, errors);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void CollectAzureMarketplaceLandingPageUrl_when_localhost_adds_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["Billing:Provider"] = BillingProviderNames.AzureMarketplace,
            ["Billing:AzureMarketplace:LandingPageUrl"] = "http://localhost:3000/marketplace/landing",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        List<string> errors = [];

        BillingProductionSafetyRules.CollectAzureMarketplaceLandingPageUrl(configuration, errors);

        errors.Should().ContainSingle(static e => e.Contains("localhost", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectAzureMarketplaceLandingPageUrl_when_https_public_host_is_clean()
    {
        Dictionary<string, string?> data = new()
        {
            ["Billing:Provider"] = BillingProviderNames.AzureMarketplace,
            ["Billing:AzureMarketplace:LandingPageUrl"] = "https://app.archlucid.com/marketplace/landing",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        List<string> errors = [];

        BillingProductionSafetyRules.CollectAzureMarketplaceLandingPageUrl(configuration, errors);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void CollectAzureMarketplaceGaRequiresOfferId_when_ga_true_and_empty_offer_adds_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["Billing:Provider"] = BillingProviderNames.AzureMarketplace,
            ["Billing:AzureMarketplace:GaEnabled"] = "true",
            ["Billing:AzureMarketplace:MarketplaceOfferId"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        List<string> errors = [];

        BillingProductionSafetyRules.CollectAzureMarketplaceGaRequiresOfferId(configuration, errors);

        errors.Should()
            .ContainSingle(static e => e.Contains("MarketplaceOfferId", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectAzureMarketplaceGaRequiresOfferId_when_ga_true_and_offer_set_is_clean()
    {
        Dictionary<string, string?> data = new()
        {
            ["Billing:Provider"] = BillingProviderNames.AzureMarketplace,
            ["Billing:AzureMarketplace:GaEnabled"] = "true",
            ["Billing:AzureMarketplace:MarketplaceOfferId"] = "contoso-archlucid-saas-offer",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        List<string> errors = [];

        BillingProductionSafetyRules.CollectAzureMarketplaceGaRequiresOfferId(configuration, errors);

        errors.Should().BeEmpty();
    }
}
