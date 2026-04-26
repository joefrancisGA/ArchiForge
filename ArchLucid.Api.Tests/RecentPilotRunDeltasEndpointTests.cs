using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Contracts.Pilots;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <c>GET /v1/pilots/runs/recent-deltas</c> — the aggregated proof-of-ROI endpoint
///     behind the BeforeAfterDeltaPanel top / sidebar variants.
/// </summary>
/// <remarks>
///     The integration host runs against an empty-by-default scope (no seeded committed runs), so the
///     contract surface we can assert here is: 200 OK, well-formed JSON envelope, count clamped to the
///     server-side bounds, and ReadAuthority gating (the integration host's DevelopmentBypass already
///     supplies a Read principal — a 401/403 here would mean we accidentally tightened the policy).
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class RecentPilotRunDeltasEndpointTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetRecentDeltas_DefaultCount_ReturnsOkWithRequestedCountFive()
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/v1/pilots/runs/recent-deltas");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        RecentPilotRunDeltasResponse? body =
            await response.Content.ReadFromJsonAsync<RecentPilotRunDeltasResponse>(JsonOptions);

        body.Should().NotBeNull();
        body.RequestedCount.Should().Be(5);
        body.ReturnedCount.Should().BeGreaterThanOrEqualTo(0);
        body.Items.Should().NotBeNull();
        body.ReturnedCount.Should().Be(body.Items.Count);
    }

    [Fact]
    public async Task GetRecentDeltas_ExplicitCount_RespectedWhenInRange()
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/v1/pilots/runs/recent-deltas?count=3");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        RecentPilotRunDeltasResponse? body =
            await response.Content.ReadFromJsonAsync<RecentPilotRunDeltasResponse>(JsonOptions);

        body.Should().NotBeNull();
        body.RequestedCount.Should().Be(3);
    }

    [Fact]
    public async Task GetRecentDeltas_ExcessiveCount_ClampedToServerSideMax()
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/v1/pilots/runs/recent-deltas?count=9999");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        RecentPilotRunDeltasResponse? body =
            await response.Content.ReadFromJsonAsync<RecentPilotRunDeltasResponse>(JsonOptions);

        body.Should().NotBeNull();
        body.RequestedCount.Should().BeLessThanOrEqualTo(25);
        body.RequestedCount.Should().BeGreaterThan(5);
    }

    [Fact]
    public async Task GetRecentDeltas_ZeroOrNegativeCount_ClampedToOne()
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/v1/pilots/runs/recent-deltas?count=-3");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        RecentPilotRunDeltasResponse? body =
            await response.Content.ReadFromJsonAsync<RecentPilotRunDeltasResponse>(JsonOptions);

        body.Should().NotBeNull();
        body.RequestedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetRecentDeltas_ResponseEnvelope_HasCamelCaseProperties()
    {
        HttpResponseMessage response = await Client.GetAsync("/v1/pilots/runs/recent-deltas?count=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string raw = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(raw);
        JsonElement root = doc.RootElement;

        root.TryGetProperty("items", out _).Should().BeTrue();
        root.TryGetProperty("requestedCount", out _).Should().BeTrue();
        root.TryGetProperty("returnedCount", out _).Should().BeTrue();
        root.TryGetProperty("medianTotalFindings", out _).Should().BeTrue();
        root.TryGetProperty("medianTimeToCommittedManifestTotalSeconds", out _).Should().BeTrue();
    }
}
