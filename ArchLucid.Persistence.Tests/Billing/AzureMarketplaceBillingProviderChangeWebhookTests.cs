using System.Globalization;
using System.Security.Claims;

using ArchLucid.Application.Billing;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Billing;
using ArchLucid.Persistence.Billing.AzureMarketplace;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Persistence.Tests.Billing;

[Trait("Category", "Unit")]
public sealed class AzureMarketplaceBillingProviderChangeWebhookTests
{
    [SkippableFact]
    public async Task ChangePlan_ga_off_skips_ledger_change_plan()
    {
        BillingOptions billing = new()
        {
            Provider = BillingProviderNames.AzureMarketplace,
            AzureMarketplace = new AzureMarketplaceBillingOptions
            {
                GaEnabled = false,
                OpenIdMetadataAddress =
                    "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                ValidAudiences = ["https://marketplaceapi.microsoft.com"]
            }
        };

        TestMonitor<BillingOptions> monitor = new(billing);
        Mock<IBillingLedger> ledger = new();
        ledger
            .Setup(l => l.TryInsertWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        ledger
            .Setup(l => l.MarkWebhookProcessedAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ITenantRepository> tenants = new();
        Mock<IAuditService> audit = new();
        BillingWebhookTrialActivator activator = new(ledger.Object, tenants.Object, audit.Object);
        Mock<IMarketplaceWebhookTokenVerifier> verifier = new();
        verifier
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaimsPrincipal());

        Mock<IHttpClientFactory> httpFactory = new();
        MarketplaceChangePlanWebhookMutationHandler changePlanHandler = new(
            monitor,
            ledger.Object,
            NullLogger<MarketplaceChangePlanWebhookMutationHandler>.Instance);
        MarketplaceChangeQuantityWebhookMutationHandler changeQtyHandler = new(
            monitor,
            ledger.Object,
            NullLogger<MarketplaceChangeQuantityWebhookMutationHandler>.Instance);
        AzureMarketplaceBillingProvider sut = new(
            monitor,
            ledger.Object,
            activator,
            verifier.Object,
            httpFactory.Object,
            changePlanHandler,
            changeQtyHandler);

        Guid tenantId = Guid.NewGuid();
        string body =
            "{\"action\":\"ChangePlan\",\"subscriptionId\":\"sub-u\",\"planId\":\"x-enterprise\",\"purchaser\":{\"tenantId\":\""
            + tenantId.ToString("D", CultureInfo.InvariantCulture)
            + "\"}}";

        BillingWebhookHandleResult result = await sut.HandleWebhookAsync(
            new BillingWebhookInbound { RawBody = body, MarketplaceAuthorizationBearer = "bearer" },
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Returns202Accepted.Should().BeTrue();

        ledger.Verify(
            static l => l.ChangePlanAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task ChangePlan_ga_on_invokes_ledger_change_plan()
    {
        BillingOptions billing = new()
        {
            Provider = BillingProviderNames.AzureMarketplace,
            AzureMarketplace = new AzureMarketplaceBillingOptions
            {
                GaEnabled = true,
                OpenIdMetadataAddress =
                    "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                ValidAudiences = ["https://marketplaceapi.microsoft.com"]
            }
        };

        TestMonitor<BillingOptions> monitor = new(billing);
        Mock<IBillingLedger> ledger = new();
        ledger
            .Setup(l => l.TryInsertWebhookEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        ledger
            .Setup(l => l.MarkWebhookProcessedAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ledger
            .Setup(l => l.ChangePlanAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<ITenantRepository> tenants = new();
        Mock<IAuditService> audit = new();
        BillingWebhookTrialActivator activator = new(ledger.Object, tenants.Object, audit.Object);
        Mock<IMarketplaceWebhookTokenVerifier> verifier = new();
        verifier
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaimsPrincipal());

        Mock<IHttpClientFactory> httpFactory = new();
        MarketplaceChangePlanWebhookMutationHandler changePlanHandler = new(
            monitor,
            ledger.Object,
            NullLogger<MarketplaceChangePlanWebhookMutationHandler>.Instance);
        MarketplaceChangeQuantityWebhookMutationHandler changeQtyHandler = new(
            monitor,
            ledger.Object,
            NullLogger<MarketplaceChangeQuantityWebhookMutationHandler>.Instance);
        AzureMarketplaceBillingProvider sut = new(
            monitor,
            ledger.Object,
            activator,
            verifier.Object,
            httpFactory.Object,
            changePlanHandler,
            changeQtyHandler);

        Guid tenantId = Guid.NewGuid();
        string body =
            "{\"action\":\"ChangePlan\",\"subscriptionId\":\"sub-u\",\"planId\":\"x-enterprise\",\"purchaser\":{\"tenantId\":\""
            + tenantId.ToString("D", CultureInfo.InvariantCulture)
            + "\"}}";

        BillingWebhookHandleResult result = await sut.HandleWebhookAsync(
            new BillingWebhookInbound { RawBody = body, MarketplaceAuthorizationBearer = "bearer" },
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Returns202Accepted.Should().BeFalse();

        ledger.Verify(
            l => l.ChangePlanAsync(tenantId, nameof(TenantTier.Enterprise), body, It.IsAny<CancellationToken>()),
            Times.Once);
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
