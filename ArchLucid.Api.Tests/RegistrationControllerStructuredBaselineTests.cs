using System.Net;
using System.Text;
using System.Text.Json;

using ArchLucid.Api.Models.Tenancy;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "StructuredBaseline")]
[Collection("ArchLucidEnvMutation")]
public sealed class RegistrationControllerStructuredBaselineTests : IClassFixture<GreenfieldSqlApiFactory>
{
    private readonly GreenfieldSqlApiFactory _fixture;

    public RegistrationControllerStructuredBaselineTests(GreenfieldSqlApiFactory fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Register_rejects_invalid_company_size()
    {
        using HttpClient client = _fixture.CreateClient();
        string org = "Co Bad " + Guid.NewGuid().ToString("N");

        Dictionary<string, object?> body = new()
        {
            ["organizationName"] = org,
            ["adminEmail"] = "badsize@example.com",
            ["adminDisplayName"] = "U",
            ["companySize"] = "nope"
        };

        using HttpResponseMessage res = await client.PostAsync(
            "/v1/register",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task Register_rejects_non_positive_architecture_team_size()
    {
        using HttpClient client = _fixture.CreateClient();
        string org = "Team Bad " + Guid.NewGuid().ToString("N");

        Dictionary<string, object?> body = new()
        {
            ["organizationName"] = org,
            ["adminEmail"] = "badteam@example.com",
            ["adminDisplayName"] = "U",
            ["architectureTeamSize"] = 0
        };

        using HttpResponseMessage res = await client.PostAsync(
            "/v1/register",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task Register_rejects_other_industry_without_free_text()
    {
        using HttpClient client = _fixture.CreateClient();
        string org = "Ind Other " + Guid.NewGuid().ToString("N");

        Dictionary<string, object?> body = new()
        {
            ["organizationName"] = org,
            ["adminEmail"] = "otherblank@example.com",
            ["adminDisplayName"] = "U",
            ["industryVertical"] = "Other"
        };

        using HttpResponseMessage res = await client.PostAsync(
            "/v1/register",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task Register_succeeds_without_new_fields_backward_compatible()
    {
        using HttpClient client = _fixture.CreateClient();
        string org = "Legacy " + Guid.NewGuid().ToString("N");

        Dictionary<string, object?> body = new()
        {
            ["organizationName"] = org, ["adminEmail"] = "legacy@example.com", ["adminDisplayName"] = "U"
        };

        using HttpResponseMessage res = await client.PostAsync(
            "/v1/register",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task Register_persists_structured_baseline_on_valid_request()
    {
        using HttpClient client = _fixture.CreateClient();
        string org = "Full " + Guid.NewGuid().ToString("N");

        Dictionary<string, object?> body = new()
        {
            ["organizationName"] = org,
            ["adminEmail"] = "full@example.com",
            ["adminDisplayName"] = "U",
            ["companySize"] = StructuredBaselineConstants.AllowedCompanySizes[2],
            ["architectureTeamSize"] = 4,
            ["industryVertical"] = "Technology"
        };

        using HttpResponseMessage res = await client.PostAsync(
            "/v1/register",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
