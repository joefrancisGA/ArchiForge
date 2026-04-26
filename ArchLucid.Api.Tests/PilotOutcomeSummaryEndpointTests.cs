using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Models.Pilots;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>HTTP coverage for <c>GET /v1/pilots/outcome-summary</c> — trailing 30-day rollup for operator home.</summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class PilotOutcomeSummaryEndpointTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetOutcomeSummary_returns_ok_with_scorecard_shape()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/pilots/outcome-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        PilotScorecardResponse? body = await response.Content.ReadFromJsonAsync<PilotScorecardResponse>(JsonOptions);

        body.Should().NotBeNull();
        body.TenantId.Should().NotBeEmpty();
        body.RunsInPeriod.Should().BeGreaterThanOrEqualTo(0);
        body.RunsWithCommittedManifest.Should().BeGreaterThanOrEqualTo(0);
        (body.PeriodEnd > body.PeriodStart).Should().BeTrue();
    }
}
