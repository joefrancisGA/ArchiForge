using System.Net;
using System.Net.Http.Headers;
using System.Text;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Billing;

/// <summary>HTTP-level tests for <c>/v1/billing/webhooks/marketplace</c> (SQL-backed ledger + stub JWT verifier).</summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class BillingMarketplaceWebhookHttpTests
{
    [Fact]
    public async Task ChangePlan_ga_disabled_returns_202_and_does_not_mutate_tier()
    {
        BillingMarketplaceWebhookDeferredApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        Guid tenantId = Guid.NewGuid();

        await BillingMarketplaceWebhookTestSeed.SeedTenantWithActiveBillingAsync(factory.SqlConnectionString, tenantId);

        string body =
            "{\"action\":\"ChangePlan\",\"subscriptionId\":\"sub-202-test\",\"planId\":\"contoso-enterprise\",\"quantity\":5,\"purchaser\":{\"tenantId\":\""
            + tenantId.ToString("D", System.Globalization.CultureInfo.InvariantCulture)
            + "\"}}";

        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/billing/webhooks/marketplace")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test-bearer");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        string tier = await BillingMarketplaceWebhookTestSeed.ReadBillingTierAsync(factory.SqlConnectionString, tenantId);

        tier.Should().Be("Standard");
    }

    [Fact]
    public async Task ChangePlan_ga_enabled_updates_tier_via_sp_Billing_ChangePlan()
    {
        BillingMarketplaceWebhookGaOnApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        Guid tenantId = Guid.NewGuid();

        await BillingMarketplaceWebhookTestSeed.SeedTenantWithActiveBillingAsync(factory.SqlConnectionString, tenantId);

        string body =
            "{\"action\":\"ChangePlan\",\"subscriptionId\":\"sub-200-test\",\"planId\":\"contoso-enterprise\",\"purchaser\":{\"tenantId\":\""
            + tenantId.ToString("D", System.Globalization.CultureInfo.InvariantCulture)
            + "\"}}";

        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/billing/webhooks/marketplace")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test-bearer");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string tier = await BillingMarketplaceWebhookTestSeed.ReadBillingTierAsync(factory.SqlConnectionString, tenantId);

        tier.Should().Be("Enterprise");
    }

    [Fact]
    public async Task ChangeQuantity_ga_enabled_updates_seats_via_sp_Billing_ChangeQuantity()
    {
        BillingMarketplaceWebhookGaOnApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        Guid tenantId = Guid.NewGuid();

        await BillingMarketplaceWebhookTestSeed.SeedTenantWithActiveBillingAsync(factory.SqlConnectionString, tenantId);

        string body =
            "{\"action\":\"ChangeQuantity\",\"subscriptionId\":\"sub-qty\",\"quantity\":42,\"purchaser\":{\"tenantId\":\""
            + tenantId.ToString("D", System.Globalization.CultureInfo.InvariantCulture)
            + "\"}}";

        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/billing/webhooks/marketplace")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test-bearer");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        int seats = await BillingMarketplaceWebhookTestSeed.ReadBillingSeatsAsync(factory.SqlConnectionString, tenantId);

        seats.Should().Be(42);
    }

    [Fact]
    public async Task ChangeQuantity_ga_disabled_returns_202_and_does_not_mutate_seats()
    {
        BillingMarketplaceWebhookDeferredApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        Guid tenantId = Guid.NewGuid();

        await BillingMarketplaceWebhookTestSeed.SeedTenantWithActiveBillingAsync(factory.SqlConnectionString, tenantId);

        string body =
            "{\"action\":\"ChangeQuantity\",\"subscriptionId\":\"sub-qty-202\",\"quantity\":99,\"purchaser\":{\"tenantId\":\""
            + tenantId.ToString("D", System.Globalization.CultureInfo.InvariantCulture)
            + "\"}}";

        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/billing/webhooks/marketplace")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test-bearer");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        int seats = await BillingMarketplaceWebhookTestSeed.ReadBillingSeatsAsync(factory.SqlConnectionString, tenantId);

        seats.Should().Be(2);
    }
}
