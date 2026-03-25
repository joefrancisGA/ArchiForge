using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureCompareExportTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CompareManifestsExport_ReturnsMarkdownExport()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-COMPARE-EXPORT-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        CommitRunResponseDto? commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        string leftVersion = commitPayload!.Manifest.Metadata.ManifestVersion;

        var replayRequest = new
        {
            commitReplay = true,
            executionMode = "Current",
            manifestVersionOverride = "v1-replay"
        };

        await Client.PostAsync($"/v1/architecture/run/{runId}/replay", JsonContent(replayRequest));

        HttpResponseMessage response = await Client.GetAsync(
            $"/v1/architecture/manifest/compare/export?leftVersion={leftVersion}&rightVersion=v1-replay");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ManifestCompareExportResponse? payload = await response.Content.ReadFromJsonAsync<ManifestCompareExportResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Format.Should().Be("markdown");
        payload.Content.Should().Contain("# ArchiForge Manifest Comparison Export");
        payload.Content.Should().Contain(leftVersion);
        payload.Content.Should().Contain("v1-replay");
    }
}
