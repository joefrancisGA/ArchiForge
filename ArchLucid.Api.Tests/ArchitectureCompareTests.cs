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

        var replayRequest = new
        {
            commitReplay = true, executionMode = "Current", manifestVersionOverride = "v1-replay"
        };

        HttpResponseMessage replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(replayRequest));

        replayResponse.EnsureSuccessStatusCode();

        HttpResponseMessage compareResponse = await Client.GetAsync(
            $"/v1/architecture/manifest/compare?leftVersion={leftVersion}&rightVersion=v1-replay");

        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ManifestCompareResponse? payload =
            await compareResponse.Content.ReadFromJsonAsync<ManifestCompareResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Diff.Should().NotBeNull();
        payload.Diff.LeftManifestVersion.Should().Be(leftVersion);
        payload.Diff.RightManifestVersion.Should().Be("v1-replay");
    }
}
