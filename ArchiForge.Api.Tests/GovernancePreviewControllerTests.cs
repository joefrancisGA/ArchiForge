using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class GovernancePreviewControllerTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CompareEnvironments_ReturnsOk()
    {
        var body = new { sourceEnvironment = "dev", targetEnvironment = "test" };
        var response = await Client.PostAsync(
            "/v1/governance-preview/compare-environments",
            JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GovernanceEnvironmentComparisonResultDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload.SourceEnvironment.Should().Be("dev");
        payload.TargetEnvironment.Should().Be("test");
        payload.Notes.Should().NotBeNull();
    }

    [Fact]
    public async Task Preview_ReturnsNotFound_WhenRunMissing()
    {
        var body = new
        {
            runId = "nonexistent-run-id-xxxxxxxx",
            manifestVersion = "v1",
            environment = "dev"
        };

        var response = await Client.PostAsync("/v1/governance-preview/preview", JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Preview_ReturnsOk_AfterCommittedRun()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-GOV-PREVIEW-01")));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();
        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();
        var commit = await commitResponse.Content.ReadFromJsonAsync<CommitRunResponseDto>(JsonOptions);
        var manifestVersion = commit!.Manifest.Metadata.ManifestVersion;

        var previewBody = new { runId, manifestVersion, environment = "dev" };
        var previewResponse = await Client.PostAsync("/v1/governance-preview/preview", JsonContent(previewBody));

        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await previewResponse.Content.ReadFromJsonAsync<GovernancePreviewResultDto>(JsonOptions);
        preview.Should().NotBeNull();
        preview.PreviewRunId.Should().Be(runId);
        preview.PreviewManifestVersion.Should().Be(manifestVersion);
        preview.Environment.Should().Be("dev");
    }
}

public sealed class GovernanceEnvironmentComparisonResultDto
{
    public string SourceEnvironment { get; set; } = string.Empty;
    public string TargetEnvironment { get; set; } = string.Empty;
    public List<string> Notes { get; set; } = [];
}

public sealed class GovernancePreviewResultDto
{
    public string Environment { get; set; } = string.Empty;
    public string PreviewRunId { get; set; } = string.Empty;
    public string PreviewManifestVersion { get; set; } = string.Empty;
}
