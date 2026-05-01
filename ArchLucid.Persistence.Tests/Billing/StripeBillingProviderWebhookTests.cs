using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Billing;
using ArchLucid.Persistence.Billing.Stripe;

using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Persistence.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class StripeBillingProviderWebhookTests
{
    [SkippableFact]
    public async Task HandleWebhookAsync_without_signature_rejected()
    {
        BillingOptions billing = new()
        {
            Provider = BillingProviderNames.Stripe,
            Stripe = new StripeBillingOptions { WebhookSigningSecret = "whsec_test" }
        };

        TestMonitor<BillingOptions> monitor = new(billing);
        Mock<IBillingLedger> ledger = new();
        Mock<ITenantRepository> tenants = new();
        Mock<IAuditService> audit = new();
        BillingWebhookTrialActivator activator = new(ledger.Object, tenants.Object, audit.Object);
        Mock<IMarketplaceChangePlanWebhookMutationHandler> changePlan = new();
        changePlan
            .Setup(h => h.HandleAsync(It.IsAny<Guid>(), It.IsAny<JsonElement>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MarketplaceWebhookMutationOutcome.Applied);
        StripeBillingProvider sut = new(monitor, ledger.Object, activator, changePlan.Object);

        BillingWebhookHandleResult result = await sut.HandleWebhookAsync(
            new BillingWebhookInbound { RawBody = "{}", StripeSignatureHeader = null },
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorDetail.Should().NotBeNullOrWhiteSpace();
        ledger.Verify(
            static l => l.TryInsertWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class TestMonitor<T>(T value) : IOptionsMonitor<T>
        where T : class
    {
        public T CurrentValue
        {
            get;
        } = value;

        public T Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable? OnChange(Action<T, string?> listener)
        {
            return null;
        }
    }
}
