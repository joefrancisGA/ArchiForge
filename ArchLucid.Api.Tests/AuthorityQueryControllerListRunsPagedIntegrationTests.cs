using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <see cref="ArchLucid.Api.Controllers.Authority.AuthorityQueryController.ListRunsByProject" />:
///     <see cref="ArchLucid.Core.Pagination.CursorPagedResponse{T}" /> (keyset + legacy page/size clamping) after a
///     committed authority run exists.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class AuthorityQueryControllerListRunsPagedIntegrationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task ListRunsByProject_without_page_returns_cursor_paged_envelope()
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
        JsonElement root = doc.RootElement;
        root.ValueKind.Should().Be(JsonValueKind.Object);
        root.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        root.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        root.GetProperty("requestedTake").GetInt32().Should().Be(20);
        root.TryGetProperty("nextCursor", out JsonElement _).Should().BeTrue();
        root.GetProperty("hasMore").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }

    [SkippableFact]
    public async Task ListRunsByProject_with_legacy_page_size_clamps_requested_take()
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
        root.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        root.GetProperty("requestedTake").GetInt32().Should().Be(10);
        root.TryGetProperty("nextCursor", out JsonElement _).Should().BeTrue();
        root.GetProperty("hasMore").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }
}
