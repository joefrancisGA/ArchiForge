using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureComparisonReplayTests : IntegrationTestBase
{
    public ArchitectureComparisonReplayTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ReplayComparison_RecreatesPersistedEndToEndComparisonAsMarkdown()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-COMP-REPLAY-001")));

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
        var replayRunId = replayPayload!.ReplayRunId;

        var compareResponse = await Client.PostAsync(
            $"/v1/architecture/run/compare/end-to-end/summary?leftRunId={runId}&rightRunId={replayRunId}",
            JsonContent(new { persist = true }));

        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        compareResponse.Headers.TryGetValues("X-ArchiForge-ComparisonRecordId", out var values).Should().BeTrue();

        var comparisonRecordId = values!.Single();

        var replayComparisonResponse = await Client.PostAsync(
            $"/v1/architecture/comparisons/{comparisonRecordId}/replay",
            JsonContent(new { format = "markdown" }));

        replayComparisonResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        replayComparisonResponse.Content.Headers.ContentType!.MediaType
            .Should().Be("text/markdown");

        var content = await replayComparisonResponse.Content.ReadAsStringAsync();
        content.Should().Contain("# ArchiForge End-to-End Replay Comparison Export");
    }
}

