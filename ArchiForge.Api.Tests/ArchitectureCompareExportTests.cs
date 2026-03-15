using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureCompareExportTests : IntegrationTestBase
{
    public ArchitectureCompareExportTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CompareManifestsExport_ReturnsMarkdownExport()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-COMPARE-EXPORT-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
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

        await Client.PostAsync($"/v1/architecture/run/{runId}/replay", JsonContent(replayRequest));

        var response = await Client.GetAsync(
            $"/v1/architecture/manifest/compare/export?leftVersion={leftVersion}&rightVersion=v1-replay");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ManifestCompareExportResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Format.Should().Be("markdown");
        payload.Content.Should().Contain("# ArchiForge Manifest Comparison Export");
        payload.Content.Should().Contain(leftVersion);
        payload.Content.Should().Contain("v1-replay");
    }
}
