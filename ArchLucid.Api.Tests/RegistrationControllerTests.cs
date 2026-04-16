using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>Self-service registration with <see cref="GreenfieldSqlApiFactory"/> (full SQL DI + DbUp + schema bootstrap).</summary>
[Trait("Suite", "Core")]
public sealed class RegistrationControllerTests : IClassFixture<GreenfieldSqlApiFactory>
{
    private readonly GreenfieldSqlApiFactory _fixture;

    public RegistrationControllerTests(GreenfieldSqlApiFactory fixture) => _fixture = fixture;

    [Fact]
    public async Task Register_creates_tenant_then_returns_conflict_for_same_organization()
    {
        using HttpClient client = _fixture.CreateClient();
        string organizationName = "Reg Org " + Guid.NewGuid().ToString("N");

        using HttpResponseMessage created = await client.PostAsync(
            "/v1/register",
            JsonContent(organizationName, "first@example.com", "First User"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        using HttpResponseMessage duplicate = await client.PostAsync(
            "/v1/register",
            JsonContent(organizationName, "second@example.com", null));

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_then_trial_status_returns_active_with_sample_run()
    {
        using HttpClient client = _fixture.CreateClient();
        string organizationName = "Trial Org " + Guid.NewGuid().ToString("N");

        using HttpResponseMessage created = await client.PostAsync(
            "/v1/register",
            JsonContent(organizationName, "trial@example.com", "Trial User"));

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        using JsonDocument doc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Guid tenantId = doc.RootElement.GetProperty("tenantId").GetGuid();

        using HttpRequestMessage statusReq = new(HttpMethod.Get, "/v1/tenant/trial-status");
        statusReq.Headers.Add("x-tenant-id", tenantId.ToString());
        using HttpResponseMessage status = await client.SendAsync(statusReq);

        status.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument statusDoc = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
        statusDoc.RootElement.GetProperty("status").GetString().Should().Be("Active");
        statusDoc.RootElement.GetProperty("trialSampleRunId").GetGuid().Should().NotBeEmpty();
    }

    private static StringContent JsonContent(string organizationName, string adminEmail, string? displayName)
    {
        Dictionary<string, string?> payload = new()
        {
            ["organizationName"] = organizationName,
            ["adminEmail"] = adminEmail,
        };

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            payload["adminDisplayName"] = displayName;
        }

        string json = JsonSerializer.Serialize(payload);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
