using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Replay.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureReplayTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task ReplayRun_ReexecutesPriorRun()
    {
        string runId =
            await ComparisonReplayTestFixture.CreateRunAndExecuteAsync(Client, JsonOptions, "REQ-REPLAY-001");

        var replayRequest = new
        {
            commitReplay = false, executionMode = "Current", manifestVersionOverride = (string?)null
        };

        HttpResponseMessage replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(replayRequest));

        replayResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ReplayRunResponseDto? payload =
            await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload.OriginalRunId.Should().Be(runId);
        payload.ReplayRunId.Should().NotBeNullOrWhiteSpace();
        payload.Results.Should().NotBeEmpty();
        payload.Manifest.Should().BeNull();
    }

    [SkippableFact]
    public async Task ReplayRun_WithCommitReplay_CreatesReplayManifest()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-REPLAY-002")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        string rightVersion = "v1-replay";

        var replayRequest = new
        {
            commitReplay = true, executionMode = "Current", manifestVersionOverride = rightVersion
        };

        HttpResponseMessage replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(replayRequest));

        replayResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ReplayRunResponseDto? payload =
            await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Manifest.Should().NotBeNull();
        payload.Manifest!.Metadata.ManifestVersion.Should().Be("v1-replay");
        payload.DecisionTraces.Should().NotBeEmpty();
    }
}
