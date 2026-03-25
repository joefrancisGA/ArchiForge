using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureAnalysisDocxTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ExportAnalysisReportDocx_ReturnsDocxFile()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DOCX-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
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
            $"/v1/architecture/run/{runId}/analysis-report/export/docx",
            JsonContent(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportAnalysisReportDocxAsync_ReturnsJob_AndEventuallyFile()
    {
        HttpResponseMessage createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-DOCX-ASYNC-001")));

        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
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

        HttpResponseMessage start = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/analysis-report/export/docx/async",
            JsonContent(request));

        start.StatusCode.Should().Be(HttpStatusCode.Accepted);

        JsonElement payload = await start.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        payload.TryGetProperty("jobId", out JsonElement jobIdEl).Should().BeTrue();
        string? jobId = jobIdEl.GetString();
        jobId.Should().NotBeNullOrWhiteSpace();

        // Poll status briefly until succeeded.
        string state = "Pending";
        for (int i = 0; i < 25; i++)
        {
            HttpResponseMessage statusResp = await Client.GetAsync($"/v1/jobs/{jobId}");
            statusResp.EnsureSuccessStatusCode();

            JsonElement status = await statusResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            state = status.GetProperty("state").GetString() ?? state;

            if (string.Equals(state, "Succeeded", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "Failed", StringComparison.OrdinalIgnoreCase))
                break;

            await Task.Delay(200);
        }

        state.Should().Be("Succeeded");

        HttpResponseMessage fileResp = await Client.GetAsync($"/v1/jobs/{jobId}/file");
        fileResp.StatusCode.Should().Be(HttpStatusCode.OK);
        fileResp.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        byte[] bytes = await fileResp.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }
}
