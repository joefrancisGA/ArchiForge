using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Compare.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArchitectureCompareTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task CompareManifests_ReturnsDiff()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-COMPARE-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        CommitRunResponseDto? commitPayload =
            await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        string leftVersion = commitPayload!.Manifest.Metadata.ManifestVersion;

        const string requestedReplayManifestVersion = "v1-replay";

        var replayRequest = new
        {
            commitReplay = true, executionMode = "Current", manifestVersionOverride = requestedReplayManifestVersion
        };

        HttpResponseMessage replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(replayRequest));

        replayResponse.EnsureSuccessStatusCode();

        ReplayRunResponseDto? replayPayload =
            await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        replayPayload.Should().NotBeNull();
        replayPayload.Manifest.Should().NotBeNull();
        string rightVersion = replayPayload.Manifest!.Metadata.ManifestVersion;
        rightVersion.Should().Be(requestedReplayManifestVersion);

        HttpResponseMessage compareResponse = await Client.GetAsync(
            $"/v1/architecture/manifest/compare?leftVersion={Uri.EscapeDataString(leftVersion)}&rightVersion={Uri.EscapeDataString(rightVersion)}");

        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK, await compareResponse.Content.ReadAsStringAsync());

        ManifestCompareResponse? payload =
            await compareResponse.Content.ReadFromJsonAsync<ManifestCompareResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Diff.Should().NotBeNull();
        payload.Diff.LeftManifestVersion.Should().Be(leftVersion);
        payload.Diff.RightManifestVersion.Should().Be(rightVersion);
    }
}
