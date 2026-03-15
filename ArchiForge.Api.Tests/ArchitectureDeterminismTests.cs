using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureDeterminismTests : IntegrationTestBase
{
    public ArchitectureDeterminismTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task DeterminismCheck_ReturnsResult()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DETERMINISM-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var request = new
        {
            iterations = 3,
            executionMode = "Current",
            commitReplays = false
        };

        var response = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/determinism-check",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DeterminismCheckResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Result.SourceRunId.Should().Be(runId);
        payload.Result.Iterations.Should().Be(3);
        payload.Result.IterationResults.Should().HaveCount(3);
    }
}
