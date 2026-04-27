using System.Net;

using ArchLucid.Api.Models.Pilots;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class PilotInProductScorecardEndpointTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetInProductScorecard_returns_ok_with_metric_shape()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/pilots/scorecard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        PilotInProductScorecardResponse? body =
            await response.Content.ReadFromJsonAsync<PilotInProductScorecardResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.TenantId.Should().NotBeEmpty();
        body.TotalRunsCommitted.Should().BeGreaterThanOrEqualTo(0);
        body.TotalManifestsCreated.Should().BeGreaterThanOrEqualTo(0);
        body.RoiEstimate.Should().BeNull();
    }
}
