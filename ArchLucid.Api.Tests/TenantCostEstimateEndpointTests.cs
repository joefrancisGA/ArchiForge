using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Models.Tenancy;
using ArchLucid.Api.Routing;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class TenantCostEstimateEndpointTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetTenantCostEstimate_returns_200_with_band()
    {
        HttpResponseMessage response = await Client.GetAsync($"/{ApiV1Routes.TenantCostEstimate}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TenantCostEstimateResponse? body =
            await response.Content.ReadFromJsonAsync<TenantCostEstimateResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Currency.Should().Be("USD");
        body.EstimatedMonthlyUsdLow.Should().BeGreaterThanOrEqualTo(0);
        body.EstimatedMonthlyUsdHigh.Should().BeGreaterThanOrEqualTo(body.EstimatedMonthlyUsdLow);
        body.Factors.Should().NotBeEmpty();
    }
}
