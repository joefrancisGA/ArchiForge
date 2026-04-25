using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <see cref="ArchLucid.Api.Controllers.Authority.AuthorityQueryController.ListRunsByProject" /> —
///     non-paged array vs. paged envelope after a committed authority run exists.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class AuthorityQueryControllerListRunsPagedIntegrationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ListRunsByProject_without_page_returns_json_array()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-AUTH-LIST-UNPAGED-001")));
        createResponse.EnsureSuccessStatusCode();
        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();
        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        HttpResponseMessage response = await Client.GetAsync("/v1/authority/projects/EnterpriseRag/runs?take=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(body);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListRunsByProject_with_page_returns_paged_envelope()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-AUTH-LIST-PAGED-001")));
        createResponse.EnsureSuccessStatusCode();
        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();
        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        HttpResponseMessage response =
            await Client.GetAsync("/v1/authority/projects/EnterpriseRag/runs?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;
        root.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        root.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("page").GetInt32().Should().Be(1);
        root.GetProperty("pageSize").GetInt32().Should().Be(10);
    }
}
