using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Analysis Report.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArchitectureAnalysisReportTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task AnalysisReport_ReturnsUnifiedReport()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-ANALYSIS-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
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

        HttpResponseMessage response = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/analysis-report",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ArchitectureAnalysisReportResponse? payload =
            await response.Content.ReadFromJsonAsync<ArchitectureAnalysisReportResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Report.Run.RunId.Should().Be(runId);
        payload.Report.Evidence.Should().NotBeNull();
        payload.Report.ExecutionTraces.Should().NotBeEmpty();
        payload.Report.Manifest.Should().NotBeNull();
        payload.Report.Diagram.Should().Contain("flowchart ");
        payload.Report.Summary.Should().Contain("# Architecture Summary:");
    }
}
