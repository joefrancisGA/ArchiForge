using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

using Microsoft.AspNetCore.WebUtilities;

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
    [SkippableFact]
    public async Task GetImprovements_unknown_run_returns_404_problem()
    {
        HttpResponseMessage response = await Client.GetAsync(
            "/v1/advisory/runs/00000000-0000-0000-0000-00000000aa01/improvements");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await AssertProblemDetailsAsync(response, ProblemTypes.RunNotFound);
    }

    [SkippableFact]
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

        Guid primaryRunId = Guid.Parse(runId);

        Guid missingCompareRunId = Guid.Parse("00000000-0000-0000-0000-00000000aa02");
        string improvementsPath = QueryHelpers.AddQueryString(
            $"/v1/advisory/runs/{primaryRunId:D}/improvements",
            "compareToRunId",
            missingCompareRunId.ToString("D"));

        HttpResponseMessage response = await Client.GetAsync(improvementsPath);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await AssertProblemDetailsAsync(response, ProblemTypes.RunNotFound, detailSubstring: "Comparison run");
    }

    /// <summary>
    ///     Asserts RFC 9457 <c>type</c> from the raw body. Deserializing <c>application/problem+json</c> into
    ///     <c>Microsoft.AspNetCore.Mvc.ProblemDetails</c> via <c>ReadFromJsonAsync</c> can yield a null or partial
    ///     <c>Type</c> when <c>extensions</c> do not round-trip cleanly; the wire <c>type</c> string remains authoritative.
    /// </summary>
    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        string expectedTypeUri,
        string? detailSubstring = null)
    {
        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNullOrWhiteSpace();

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        root.TryGetProperty("type", out JsonElement typeElement).Should().BeTrue($"expected 'type' in body: {json}");
        typeElement.GetString().Should().Be(expectedTypeUri);

        if (detailSubstring is null)
            return;

        root.TryGetProperty("detail", out JsonElement detailElement).Should().BeTrue($"expected 'detail' in body: {json}");
        string? detailText = detailElement.GetString();
        detailText.Should().NotBeNull();
        detailText.Should().Contain(detailSubstring);
    }
}
