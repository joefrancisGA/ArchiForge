using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ArchiForge.Api.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureEndToEndComparisonExportTests(ArchiForgeApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ExportRunsEndToEndComparisonMarkdown_ReturnsMarkdown()
    {
        var (runId, replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-E2E-EXPORT-001");

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
        var (runId, replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-E2E-EXPORT-002");

        var response = await Client.GetAsync(
            $"/v1/architecture/run/compare/end-to-end/export/docx?leftRunId={runId}&rightRunId={replayRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DownloadEndToEndComparisonMarkdown_RangeRequest_Returns206PartialContent()
    {
        var (runId, replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-E2E-RANGE-001");

        var url =
            $"/v1/architecture/run/compare/end-to-end/export/file?leftRunId={runId}&rightRunId={replayRunId}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Range = new RangeHeaderValue(0, 15);

        var response = await Client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        response.Headers.AcceptRanges.ToString().Should().Contain("bytes");
        response.Content.Headers.ContentRange.Should().NotBeNull();
        var body = await response.Content.ReadAsByteArrayAsync();
        body.Length.Should().Be(16);
    }
}

