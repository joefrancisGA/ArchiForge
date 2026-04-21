using System.Text.Json;

using ArchLucid.Application.Billing;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace ArchLucid.Application.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class MarketplaceChangeQuantityWebhookMutationHandlerTests
{
    [Fact]
    public async Task Ga_disabled_returns_deferred_without_ledger_mutation()
    {
        BillingOptions billing = new()
        {
            AzureMarketplace = new AzureMarketplaceBillingOptions { GaEnabled = false },
        };

        BillingOptionsTestMonitor<BillingOptions> monitor = new(billing);
        Mock<IBillingLedger> ledger = new();
        MarketplaceChangeQuantityWebhookMutationHandler sut = new(
            monitor,
            ledger.Object,
            NullLogger<MarketplaceChangeQuantityWebhookMutationHandler>.Instance);

        using JsonDocument doc = JsonDocument.Parse("""{"quantity":99}""");

        MarketplaceWebhookMutationOutcome outcome =
            await sut.HandleAsync(Guid.NewGuid(), doc.RootElement, "{}", CancellationToken.None);

        outcome.Should().Be(MarketplaceWebhookMutationOutcome.DeferredGaDisabled);

        ledger.Verify(
            static l => l.ChangeQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Ga_enabled_invokes_change_quantity_with_parsed_seats()
    {
        BillingOptions billing = new()
        {
            AzureMarketplace = new AzureMarketplaceBillingOptions { GaEnabled = true },
        };

        BillingOptionsTestMonitor<BillingOptions> monitor = new(billing);
        Mock<IBillingLedger> ledger = new();
        ledger
            .Setup(l => l.ChangeQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MarketplaceChangeQuantityWebhookMutationHandler sut = new(
            monitor,
            ledger.Object,
            NullLogger<MarketplaceChangeQuantityWebhookMutationHandler>.Instance);

        Guid tenantId = Guid.NewGuid();
        string raw = """{"quantity":15}""";
        using JsonDocument doc = JsonDocument.Parse(raw);

        MarketplaceWebhookMutationOutcome outcome =
            await sut.HandleAsync(tenantId, doc.RootElement, raw, CancellationToken.None);

        outcome.Should().Be(MarketplaceWebhookMutationOutcome.Applied);

        ledger.Verify(l => l.ChangeQuantityAsync(tenantId, 15, raw, It.IsAny<CancellationToken>()), Times.Once);
    }

}
