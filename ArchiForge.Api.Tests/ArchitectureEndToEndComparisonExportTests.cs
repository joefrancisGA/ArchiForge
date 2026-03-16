using System.Net;
using System.Net.Http.Json;
using ArchiForge.Api.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureEndToEndComparisonExportTests : IntegrationTestBase
{
    public ArchitectureEndToEndComparisonExportTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ExportRunsEndToEndComparisonMarkdown_ReturnsMarkdown()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-E2E-EXPORT-001")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        var replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(new
            {
                commitReplay = true,
                executionMode = "Current",
                manifestVersionOverride = "v1-replay"
            }));

        replayResponse.EnsureSuccessStatusCode();

        var replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        var replayRunId = replayPayload!.ReplayRunId;

        var response = await Client.GetAsync(
            $"/v1/architecture/run/compare/end-to-end/export?leftRunId={runId}&rightRunId={replayRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EndToEndReplayComparisonExportResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Format.Should().Be("markdown");
        payload.Content.Should().Contain("# ArchiForge End-to-End Replay Comparison Export");
    }

    [Fact]
    public async Task ExportRunsEndToEndComparisonDocx_ReturnsDocx()
    {
        var createResponse = await Client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest("REQ-E2E-EXPORT-002")));

        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        var replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(new
            {
                commitReplay = true,
                executionMode = "Current",
                manifestVersionOverride = "v1-replay"
            }));

        replayResponse.EnsureSuccessStatusCode();

        var replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        var replayRunId = replayPayload!.ReplayRunId;

        var response = await Client.GetAsync(
            $"/v1/architecture/run/compare/end-to-end/export/docx?leftRunId={runId}&rightRunId={replayRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }
}

