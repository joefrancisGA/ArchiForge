using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

namespace ArchLucid.Api.Tests.Security;

/// <summary>
///     Systematic API-key RBAC checks: Reader cannot execute or admin; anonymous gets 401; health read paths behave as
///     documented.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
public sealed class AuthorizationBoundaryTests(ApiKeyReaderAndAdminArchLucidApiFactory factory)
    : IClassFixture<ApiKeyReaderAndAdminArchLucidApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [SkippableFact]
    public async Task Reader_key_cannot_POST_architecture_request_returns_403()
    {
        using HttpClient client = CreateReaderClient();
        string body = JsonSerializer.Serialize(TestRequestFactory.CreateArchitectureRequest("REQ-BDR-POST-001"),
            JsonOptions);
        using HttpResponseMessage response = await client.PostAsync(
            "/v1/architecture/request",
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task Reader_key_cannot_POST_run_commit_returns_403()
    {
        using HttpClient client = CreateReaderClient();
        using HttpResponseMessage response = await client.PostAsync(
            "/v1/architecture/run/00000000-0000-0000-0000-000000000000/commit",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task Reader_key_cannot_POST_demo_seed_returns_403()
    {
        using HttpClient client = CreateReaderClient();
        using HttpResponseMessage response = await client.PostAsync("/v1/demo/seed", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task Reader_key_GET_run_returns_200_or_404_not_403()
    {
        using HttpClient client = CreateReaderClient();
        using HttpResponseMessage response =
            await client.GetAsync("/v1/architecture/run/00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task Reader_key_cannot_GET_admin_config_summary_returns_403()
    {
        using HttpClient client = CreateReaderClient();
        using HttpResponseMessage response = await client.GetAsync("/v1/admin/config-summary");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task No_api_key_on_protected_list_runs_returns_401()
    {
        using HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);
        using HttpResponseMessage response = await client.GetAsync("/v1/architecture/runs?limit=1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task Valid_reader_api_key_on_health_ready_returns_200()
    {
        using HttpClient client = CreateReaderClient();
        using HttpResponseMessage response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Valid_reader_api_key_on_detailed_health_returns_200()
    {
        using HttpClient client = CreateReaderClient();
        using HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private HttpClient CreateReaderClient()
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key",
            ApiKeyReaderAndAdminArchLucidApiFactory.IntegrationTestReaderApiKey);
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);
        return client;
    }
}
