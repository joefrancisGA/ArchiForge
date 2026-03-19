using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureAnalysisExportTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ExportAnalysisReport_ReturnsMarkdown()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-ANALYSIS-EXPORT-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        var request = new
        {
            includeEvidence = true,
            includeExecutionTraces = true,
            includeManifest = true,
            includeDiagram = true,
            includeSummary = true,
            includeDeterminismCheck = false,
            determinismIterations = 3,
            includeManifestCompare = false,
            compareManifestVersion = (string?)null,
            includeAgentResultCompare = false,
            compareRunId = (string?)null
        };

        var response = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/analysis-report/export",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ArchitectureAnalysisExportResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.RunId.Should().Be(runId);
        payload.Format.Should().Be("markdown");
        payload.FileName.Should().Be($"analysis_{runId}.md");
        payload.Content.Should().NotBeNullOrWhiteSpace();

        var lines = payload.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines[0].Trim().Should().Be("# ArchiForge Analysis Report");

        payload.Content.Should().Contain("## Evidence Package");
        payload.Content.Should().Contain("## Architecture Manifest");
        payload.Content.Should().Contain("## Agent Execution Traces");
        payload.Content.Should().Contain("## Architecture Summary");
    }
}
