using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Billing;

/// <summary>
///     Asserts the GA webhook path reaches the SQL ledger layer (Dapper SP mapping) via the ledger dispatch recorder
///     test double.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class BillingMarketplaceWebhookLedgerDispatchHttpTests
{
    [Fact]
    public async Task ChangePlan_ga_on_records_sp_Billing_ChangePlan_on_ledger_dispatch()
    {
        BillingMarketplaceWebhookRecordedLedgerApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        Guid tenantId = Guid.NewGuid();

        await BillingMarketplaceWebhookTestSeed.SeedTenantWithActiveBillingAsync(factory.SqlConnectionString, tenantId);

        string body =
            "{\"action\":\"ChangePlan\",\"subscriptionId\":\"sub-rec\",\"planId\":\"contoso-enterprise\",\"purchaser\":{\"tenantId\":\""
            + tenantId.ToString("D", CultureInfo.InvariantCulture)
            + "\"}}";

        using HttpRequestMessage request = new(HttpMethod.Post, "/v1/billing/webhooks/marketplace")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test-bearer");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        factory.RecordedStoredProcedureLogicalNames.Should().Contain("dbo.sp_Billing_ChangePlan");
    }
}
