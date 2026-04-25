using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Models.Tenancy;
using ArchLucid.Api.Routing;
using ArchLucid.Application.Bootstrap;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class TenantMeasuredRoiEndpointTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetTenantMeasuredRoi_returns_200_with_snapshot_and_disclaimer()
    {
        HttpResponseMessage response = await Client.GetAsync($"/{ApiV1Routes.TenantMeasuredRoi}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TenantMeasuredRoiResponse? body =
            await response.Content.ReadFromJsonAsync<TenantMeasuredRoiResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.Disclaimer.Should().NotBeNullOrWhiteSpace();
        body.Snapshot.Should().NotBeNull();
        body.Snapshot.DemoRunId.Should().Be(ContosoRetailDemoIdentifiers.RunBaseline);
        body.Snapshot.RunsCreatedTotal.Should().BeGreaterThanOrEqualTo(0);
    }
}
