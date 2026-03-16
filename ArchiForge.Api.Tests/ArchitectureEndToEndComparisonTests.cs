using System.Net;
using System.Net.Http.Json;
using ArchiForge.Api.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureEndToEndComparisonTests : IntegrationTestBase
{
    public ArchitectureEndToEndComparisonTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CompareRunsEndToEndSummary_ReturnsUnifiedSummary()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-E2E-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        var replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(new
            {
                commitReplay = true,
                executionMode = "Current",
                manifestVersionOverride = "v1-replay"
            }));

        replayResponse.EnsureSuccessStatusCode();

        var replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        replayPayload.Should().NotBeNull();
        var replayRunId = replayPayload!.ReplayRunId;

        var response = await Client.GetAsync(
            $"/v1/architecture/run/compare/end-to-end/summary?leftRunId={runId}&rightRunId={replayRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EndToEndReplayComparisonSummaryResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Summary.Should().Contain("# End-to-End Replay Comparison:");
    }
}

