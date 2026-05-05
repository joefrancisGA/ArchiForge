using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Models.Analytics;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>HTTP coverage for <c>GET /v1/analytics/roi</c> — mocked executive ROI aggregates.</summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class RoiAnalyticsEndpointTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task GetRoiAggregates_returns_ok_with_expected_shape()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/analytics/roi");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ExecutiveRoiAggregatesResponse? body =
            await response.Content.ReadFromJsonAsync<ExecutiveRoiAggregatesResponse>(JsonOptions);

        body.Should().NotBeNull();
        body!.TimeSavedHours.Should().BeGreaterThan(0);
        body.DecisionsAutomated.Should().BeGreaterThanOrEqualTo(0);
        body.ComplianceRisksMitigated.Should().BeGreaterThanOrEqualTo(0);
    }
}
