using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureAnalysisReportTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task AnalysisReport_ReturnsUnifiedReport()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-ANALYSIS-001")));

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
            $"/v1/architecture/run/{runId}/analysis-report",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ArchitectureAnalysisReportResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Report.Run.RunId.Should().Be(runId);
        payload.Report.Evidence.Should().NotBeNull();
        payload.Report.ExecutionTraces.Should().NotBeEmpty();
        payload.Report.Manifest.Should().NotBeNull();
        payload.Report.Diagram.Should().Contain("flowchart TD");
        payload.Report.Summary.Should().Contain("# Architecture Summary:");
    }
}
