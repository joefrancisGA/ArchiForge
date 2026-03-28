using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using ArchiForge.Api.Models;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureEndToEndComparisonExportTests(ArchiForgeApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ExportRunsEndToEndComparisonMarkdown_ReturnsMarkdown()
    {
        (string runId, string replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-E2E-EXPORT-001");

        HttpResponseMessage response = await Client.GetAsync(
            $"/v1/architecture/run/compare/end-to-end/export?leftRunId={runId}&rightRunId={replayRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        EndToEndReplayComparisonExportResponse? payload = await response.Content.ReadFromJsonAsync<EndToEndReplayComparisonExportResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Format.Should().Be("markdown");
        payload.Content.Should().Contain("# ArchiForge End-to-End Replay Comparison Export");
    }

    [Fact]
    public async Task ExportRunsEndToEndComparisonDocx_ReturnsDocx()
    {
        (string runId, string replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-E2E-EXPORT-002");

        HttpResponseMessage response = await Client.GetAsync(
            $"/v1/architecture/run/compare/end-to-end/export/docx?leftRunId={runId}&rightRunId={replayRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DownloadEndToEndComparisonMarkdown_RangeRequest_Returns206PartialContent()
    {
        (string runId, string replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-E2E-RANGE-001");

        string url =
            $"/v1/architecture/run/compare/end-to-end/export/file?leftRunId={runId}&rightRunId={replayRunId}";
        using HttpRequestMessage req = new(HttpMethod.Get, url);
        req.Headers.Range = new RangeHeaderValue(0, 15);

        HttpResponseMessage response = await Client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        response.Headers.AcceptRanges.ToString().Should().Contain("bytes");
        response.Content.Headers.ContentRange.Should().NotBeNull();
        byte[] body = await response.Content.ReadAsByteArrayAsync();
        body.Length.Should().Be(16);
    }
}

