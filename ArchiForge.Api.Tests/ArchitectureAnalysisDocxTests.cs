using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureAnalysisDocxTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ExportAnalysisReportDocx_ReturnsDocxFile()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DOCX-001")));

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
            $"/v1/architecture/run/{runId}/analysis-report/export/docx",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportAnalysisReportDocxAsync_ReturnsJob_AndEventuallyFile()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DOCX-ASYNC-001")));

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

        var start = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/analysis-report/export/docx/async",
            JsonContent(request));

        start.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payload = await start.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        payload.TryGetProperty("jobId", out var jobIdEl).Should().BeTrue();
        var jobId = jobIdEl.GetString();
        jobId.Should().NotBeNullOrWhiteSpace();

        // Poll status briefly until succeeded.
        string state = "Pending";
        for (var i = 0; i < 25; i++)
        {
            var statusResp = await Client.GetAsync($"/v1/jobs/{jobId}");
            statusResp.EnsureSuccessStatusCode();

            var status = await statusResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            state = status.GetProperty("state").GetString() ?? state;

            if (string.Equals(state, "Succeeded", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "Failed", StringComparison.OrdinalIgnoreCase))
                break;

            await Task.Delay(200);
        }

        state.Should().Be("Succeeded");

        var fileResp = await Client.GetAsync($"/v1/jobs/{jobId}/file");
        fileResp.StatusCode.Should().Be(HttpStatusCode.OK);
        fileResp.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var bytes = await fileResp.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }
}
