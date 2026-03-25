using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureAgentCompareTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CompareAgentResultsSummary_ReturnsMarkdown()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-AGENT-COMPARE-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);

        var replayRequest = new
        {
            commitReplay = false,
            executionMode = "Current",
            manifestVersionOverride = (string?)null
        };

        HttpResponseMessage replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(replayRequest));

        replayResponse.EnsureSuccessStatusCode();

        ReplayRunResponseDto? replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        string replayRunId = replayPayload!.ReplayRunId;

        HttpResponseMessage compareResponse = await Client.GetAsync(
            $"/v1/architecture/run/compare/agents/summary?leftRunId={runId}&rightRunId={replayRunId}");

        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        AgentResultCompareSummaryResponse? payload = await compareResponse.Content.ReadFromJsonAsync<AgentResultCompareSummaryResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Format.Should().Be("markdown");
        payload.Summary.Should().Contain("# Agent Result Comparison");
        payload.Diff.LeftRunId.Should().Be(runId);
        payload.Diff.RightRunId.Should().Be(replayRunId);
    }
}
