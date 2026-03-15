using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureCompareTests : IntegrationTestBase
{
    public ArchitectureCompareTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CompareManifests_ReturnsDiff()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-COMPARE-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        var leftVersion = commitPayload!.Manifest.Metadata.ManifestVersion;

        var replayRequest = new
        {
            commitReplay = true,
            executionMode = "Current",
            manifestVersionOverride = "v1-replay"
        };

        var replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(replayRequest));

        replayResponse.EnsureSuccessStatusCode();

        var compareResponse = await Client.GetAsync(
            $"/v1/architecture/manifest/compare?leftVersion={leftVersion}&rightVersion=v1-replay");

        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await compareResponse.Content.ReadFromJsonAsync<ManifestCompareResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Diff.Should().NotBeNull();
        payload.Diff.LeftManifestVersion.Should().Be(leftVersion);
        payload.Diff.RightManifestVersion.Should().Be("v1-replay");
    }
}
