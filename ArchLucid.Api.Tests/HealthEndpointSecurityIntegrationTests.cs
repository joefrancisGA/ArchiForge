using System.Linq;
using System.Net;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Regression: anonymous callers must not receive detailed health payloads on <c>/health/ready</c>; <c>/health</c> requires ReadAuthority.
/// </summary>
[Trait("Category", "Integration")]
public sealed class HealthEndpointSecurityIntegrationTests(HealthEndpointSecurityApiFactory factory)
    : IClassFixture<HealthEndpointSecurityApiFactory>
{
    [Fact]
    public async Task HealthReady_anonymous_returns_summary_without_error_or_version_fields()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;

        // CD script scripts/ci/cd-post-deploy-verify.sh expects top-level status (summary writer).
        root.GetProperty("status").GetString().Should().NotBeNullOrWhiteSpace();

        root.TryGetProperty("version", out _).Should().BeFalse("anonymous readiness must not expose build version");
        root.TryGetProperty("commitSha", out _).Should().BeFalse();
        root.TryGetProperty("totalDurationMs", out _).Should().BeFalse();

        foreach (JsonElement entry in root.GetProperty("entries").EnumerateArray())
        {
            entry.TryGetProperty("error", out _).Should().BeFalse("summary entries must not expose exception text");
            entry.TryGetProperty("description", out _).Should().BeFalse();
            entry.TryGetProperty("durationMs", out _).Should().BeFalse();
            entry.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
            entry.GetProperty("status").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Health_anonymous_returns_401()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_with_api_key_returns_detailed_payload()
    {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", HealthEndpointSecurityApiFactory.IntegrationTestAdminApiKey);

        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;

        root.TryGetProperty("version", out JsonElement version).Should().BeTrue();
        version.GetString().Should().NotBeNullOrWhiteSpace();
        root.TryGetProperty("commitSha", out _).Should().BeTrue();
        root.TryGetProperty("totalDurationMs", out _).Should().BeTrue();

        JsonElement first = root.GetProperty("entries")[0];
        first.TryGetProperty("durationMs", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Health_with_api_key_includes_circuit_breakers_entry_with_data()
    {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", HealthEndpointSecurityApiFactory.IntegrationTestAdminApiKey);

        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;

        JsonElement circuitEntry = root
            .GetProperty("entries")
            .EnumerateArray()
            .First(e =>
                string.Equals(
                    e.GetProperty("name").GetString(),
                    "circuit_breakers",
                    StringComparison.Ordinal));

        circuitEntry.GetProperty("status").GetString().Should().Be("Healthy");
        circuitEntry.TryGetProperty("data", out JsonElement data).Should().BeTrue("detailed health must surface HealthCheckResult.Data for operators");
        data.TryGetProperty("gates", out JsonElement gates).Should().BeTrue();
        gates.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
