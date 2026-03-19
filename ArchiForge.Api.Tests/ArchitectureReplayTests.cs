using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureReplayTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ReplayRun_ReexecutesPriorRun()
    {
        var runId = await ComparisonReplayTestFixture.CreateRunAndExecuteAsync(Client, JsonOptions, "REQ-REPLAY-001");

        var replayRequest = new
        {
            commitReplay = false,
            executionMode = "Current",
            manifestVersionOverride = (string?)null
        };

        var replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(replayRequest));

        replayResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.OriginalRunId.Should().Be(runId);
        payload.ReplayRunId.Should().NotBeNullOrWhiteSpace();
        payload.Results.Should().NotBeEmpty();
        payload.Manifest.Should().BeNull();
    }

    [Fact]
    public async Task ReplayRun_WithCommitReplay_CreatesReplayManifest()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-REPLAY-002")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var replayRequest = new
        {
            commitReplay = true,
            executionMode = "Current",
            manifestVersionOverride = "v1-replay"
        };

        var replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(replayRequest));

        replayResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Manifest.Should().NotBeNull();
        payload.Manifest!.Metadata.ManifestVersion.Should().Be("v1-replay");
        payload.DecisionTraces.Should().NotBeEmpty();
    }
}
