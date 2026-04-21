using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Collection("ArchLucidEnvMutation")]
public sealed class RegistrationControllerBaselineCaptureTests : IClassFixture<GreenfieldSqlApiFactory>
{
    private readonly GreenfieldSqlApiFactory _fixture;

    public RegistrationControllerBaselineCaptureTests(GreenfieldSqlApiFactory fixture) => _fixture = fixture;

    [Fact]
    public async Task Register_with_baseline_persists_to_trial_status()
    {
        using HttpClient client = _fixture.CreateClient();
        string organizationName = "Baseline Org " + Guid.NewGuid().ToString("N");

        using HttpResponseMessage created = await client.PostAsync(
            "/v1/register",
            JsonContent(
                organizationName,
                "baseline@example.com",
                "Baseline User",
                baselineHours: 18m,
                baselineSource: "team estimate"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        using JsonDocument doc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Guid tenantId = doc.RootElement.GetProperty("tenantId").GetGuid();

        using HttpRequestMessage statusReq = new(HttpMethod.Get, "/v1/tenant/trial-status");
        statusReq.Headers.Add("x-tenant-id", tenantId.ToString());
        using HttpResponseMessage status = await client.SendAsync(statusReq);

        status.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument statusDoc = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
        statusDoc.RootElement.GetProperty("baselineReviewCycleHours").GetDecimal().Should().Be(18m);
        statusDoc.RootElement.GetProperty("baselineReviewCycleSource").GetString().Should().Be("team estimate");
        string? captured = statusDoc.RootElement.GetProperty("baselineReviewCycleCapturedUtc").GetString();
        captured.Should().NotBeNullOrWhiteSpace();
        DateTimeOffset.Parse(captured, CultureInfo.InvariantCulture).Should().NotBe(default);
    }

    [Fact]
    public async Task Register_without_baseline_allows_trial_status()
    {
        using HttpClient client = _fixture.CreateClient();
        string organizationName = "No Baseline Org " + Guid.NewGuid().ToString("N");

        using HttpResponseMessage created = await client.PostAsync(
            "/v1/register",
            JsonContent(organizationName, "nobase@example.com", "User", null, null));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        using JsonDocument doc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Guid tenantId = doc.RootElement.GetProperty("tenantId").GetGuid();

        using HttpRequestMessage statusReq = new(HttpMethod.Get, "/v1/tenant/trial-status");
        statusReq.Headers.Add("x-tenant-id", tenantId.ToString());
        using HttpResponseMessage status = await client.SendAsync(statusReq);

        status.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument statusDoc = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
        statusDoc.RootElement.TryGetProperty("baselineReviewCycleHours", out JsonElement h).Should().BeTrue();
        h.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Register_rejects_baseline_out_of_range()
    {
        using HttpClient client = _fixture.CreateClient();

        using HttpResponseMessage bad = await client.PostAsync(
            "/v1/register",
            JsonContent("Bad Baseline " + Guid.NewGuid().ToString("N"), "bad@example.com", null, 0m, null));

        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_rejects_source_without_hours()
    {
        using HttpClient client = _fixture.CreateClient();

        Dictionary<string, object?> payload = new()
        {
            ["organizationName"] = "Src Only " + Guid.NewGuid().ToString("N"),
            ["adminEmail"] = "src@example.com",
            ["baselineReviewCycleSource"] = "orphan source",
        };

        using HttpResponseMessage bad = await client.PostAsync(
            "/v1/register",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static StringContent JsonContent(
        string organizationName,
        string adminEmail,
        string? displayName,
        decimal? baselineHours,
        string? baselineSource)
    {
        Dictionary<string, object?> payload = new()
        {
            ["organizationName"] = organizationName,
            ["adminEmail"] = adminEmail,
        };

        if (!string.IsNullOrWhiteSpace(displayName))
            payload["adminDisplayName"] = displayName;

        if (baselineHours is not null)
            payload["baselineReviewCycleHours"] = baselineHours.Value;

        if (baselineSource is not null)
            payload["baselineReviewCycleSource"] = baselineSource;

        string json = JsonSerializer.Serialize(payload);

        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
