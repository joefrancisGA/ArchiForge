using System.Text.Json;

using ArchLucid.Application.Billing;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class MarketplaceChangePlanWebhookMutationHandlerTests
{
    [SkippableFact]
    public async Task Ga_disabled_returns_deferred_without_ledger_mutation()
    {
        BillingOptions billing = new()
        {
            Provider = BillingProviderNames.AzureMarketplace, AzureMarketplace = new AzureMarketplaceBillingOptions { GaEnabled = false },
        };

        BillingOptionsTestMonitor<BillingOptions> monitor = new(billing);
        Mock<IBillingLedger> ledger = new();
        MarketplaceChangePlanWebhookMutationHandler sut = new(
            monitor,
            ledger.Object,
            NullLogger<MarketplaceChangePlanWebhookMutationHandler>.Instance);

        using JsonDocument doc = JsonDocument.Parse("""{"planId":"p-enterprise"}""");

        MarketplaceWebhookMutationOutcome outcome =
            await sut.HandleAsync(Guid.NewGuid(), doc.RootElement, "{}", CancellationToken.None);

        outcome.Should().Be(MarketplaceWebhookMutationOutcome.DeferredGaDisabled);

        ledger.Verify(
            static l => l.ChangePlanAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task Ga_enabled_invokes_change_plan_with_mapped_tier()
    {
        BillingOptions billing = new()
        {
            Provider = BillingProviderNames.AzureMarketplace, AzureMarketplace = new AzureMarketplaceBillingOptions { GaEnabled = true },
        };

        BillingOptionsTestMonitor<BillingOptions> monitor = new(billing);
        Mock<IBillingLedger> ledger = new();
        ledger
            .Setup(l => l.ChangePlanAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MarketplaceChangePlanWebhookMutationHandler sut = new(
            monitor,
            ledger.Object,
            NullLogger<MarketplaceChangePlanWebhookMutationHandler>.Instance);

        Guid tenantId = Guid.NewGuid();
        string raw = "{\"planId\":\"x-enterprise\"}";
        using JsonDocument doc = JsonDocument.Parse(raw);

        MarketplaceWebhookMutationOutcome outcome =
            await sut.HandleAsync(tenantId, doc.RootElement, raw, CancellationToken.None);

        outcome.Should().Be(MarketplaceWebhookMutationOutcome.Applied);

        ledger.Verify(l => l.ChangePlanAsync(tenantId, nameof(TenantTier.Enterprise), raw, It.IsAny<CancellationToken>()), Times.Once);
    }

    [SkippableFact]
    public async Task Stripe_provider_applies_change_plan_when_azure_marketplace_ga_disabled()
    {
        BillingOptions billing = new() { Provider = BillingProviderNames.Stripe, AzureMarketplace = new AzureMarketplaceBillingOptions { GaEnabled = false }, };

        BillingOptionsTestMonitor<BillingOptions> monitor = new(billing);
        Mock<IBillingLedger> ledger = new();
        ledger
            .Setup(l => l.ChangePlanAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MarketplaceChangePlanWebhookMutationHandler sut = new(
            monitor,
            ledger.Object,
            NullLogger<MarketplaceChangePlanWebhookMutationHandler>.Instance);

        Guid tenantId = Guid.NewGuid();
        string raw = """{"planId":"archlucid-stripe-team"}""";
        using JsonDocument doc = JsonDocument.Parse(raw);

        MarketplaceWebhookMutationOutcome outcome =
            await sut.HandleAsync(tenantId, doc.RootElement, raw, CancellationToken.None);

        outcome.Should().Be(MarketplaceWebhookMutationOutcome.Applied);

        ledger.Verify(l => l.ChangePlanAsync(tenantId, nameof(TenantTier.Standard), raw, It.IsAny<CancellationToken>()), Times.Once);
    }
}
