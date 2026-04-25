using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Determinism.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureDeterminismTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task DeterminismCheck_ReturnsResult()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DETERMINISM-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var request = new { iterations = 3, executionMode = "Current", commitReplays = false };

        HttpResponseMessage response = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/determinism-check",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        DeterminismCheckResponse? payload =
            await response.Content.ReadFromJsonAsync<DeterminismCheckResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Result.SourceRunId.Should().Be(runId);
        payload.Result.Iterations.Should().Be(3);
        payload.Result.IterationResults.Should().HaveCount(3);
    }
}
