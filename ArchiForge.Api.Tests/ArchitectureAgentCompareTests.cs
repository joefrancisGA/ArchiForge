using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureAgentCompareTests : IntegrationTestBase
{
    public ArchitectureAgentCompareTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CompareAgentResultsSummary_ReturnsMarkdown()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-AGENT-COMPARE-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);

        var replayRequest = new
        {
            commitReplay = false,
            executionMode = "Current",
            manifestVersionOverride = (string?)null
        };

        var replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(replayRequest));

        replayResponse.EnsureSuccessStatusCode();

        var replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        var replayRunId = replayPayload!.ReplayRunId;

        var compareResponse = await Client.GetAsync(
            $"/v1/architecture/run/compare/agents/summary?leftRunId={runId}&rightRunId={replayRunId}");

        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await compareResponse.Content.ReadFromJsonAsync<AgentResultCompareSummaryResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Format.Should().Be("markdown");
        payload.Summary.Should().Contain("# Agent Result Comparison");
        payload.Diff.LeftRunId.Should().Be(runId);
        payload.Diff.RightRunId.Should().Be(replayRunId);
    }
}
