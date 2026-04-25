using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <see cref="ArchLucid.Api.Controllers.Advisory.AdvisoryController" /> improvements path
///     (<c>GET /v1/advisory/runs/{runId}/improvements</c>) — lowest-covered API surface per coverage gap analysis.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class AdvisoryControllerImprovementsIntegrationTests(ArchLucidApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetImprovements_unknown_run_returns_404_problem()
    {
        HttpResponseMessage response = await Client.GetAsync(
            "/v1/advisory/runs/00000000-0000-0000-0000-00000000aa01/improvements");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.RunNotFound);
    }

    [Fact]
    public async Task GetImprovements_compareTo_missing_run_returns_404_run_problem()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-ADV-IMPROVE-001")));
        createResponse.EnsureSuccessStatusCode();
        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();
        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        Guid missingCompareRunId = Guid.Parse("00000000-0000-0000-0000-00000000aa02");
        HttpResponseMessage response = await Client.GetAsync(
            $"/v1/advisory/runs/{runId}/improvements?compareToRunId={missingCompareRunId:D}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        MvcProblemDetails? problem = await response.Content.ReadFromJsonAsync<MvcProblemDetails>(JsonOptions);
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemTypes.RunNotFound);
    }
}
