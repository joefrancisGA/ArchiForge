using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Contracts;
using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// HTTP coverage for <see cref="ArchLucid.Api.Controllers.Advisory.AdvisoryController.ListRecommendations"/>
/// (<c>GET /v1/advisory/runs/{runId}/recommendations</c>).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class AdvisoryControllerListRecommendationsIntegrationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ListRecommendations_for_run_with_no_rows_returns_ok_empty_array()
    {
        Guid unusedRunId = Guid.Parse("00000000-0000-0000-0000-00000000bb01");

        HttpResponseMessage response = await Client.GetAsync(
            $"/v1/advisory/runs/{unusedRunId:D}/recommendations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<RecommendationRecordResponse>? items =
            await response.Content.ReadFromJsonAsync<List<RecommendationRecordResponse>>(JsonOptions);

        items.Should().NotBeNull();
        items!.Should().BeEmpty();
    }

    [Fact]
    public async Task ListRecommendations_after_commit_before_improvements_returns_ok_empty_array()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-ADV-REC-LIST-001")));
        createResponse.EnsureSuccessStatusCode();
        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();
        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        HttpResponseMessage response = await Client.GetAsync($"/v1/advisory/runs/{runId}/recommendations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<RecommendationRecordResponse>? items =
            await response.Content.ReadFromJsonAsync<List<RecommendationRecordResponse>>(JsonOptions);

        items.Should().NotBeNull();
        items!.Should().BeEmpty();
    }
}
