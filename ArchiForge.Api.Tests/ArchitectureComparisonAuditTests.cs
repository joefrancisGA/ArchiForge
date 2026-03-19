using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureComparisonAuditTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    private sealed class ReplayRunResponseDto
    {
        public string ReplayRunId { get; set; } = string.Empty;
    }

    [Fact]
    public async Task EndToEndComparisonSummary_PersistsComparisonRecord()
    {
        var request = TestRequestFactory.CreateArchitectureRequest("REQ-COMP-AUDIT-001");

        var createResponse = await Client.PostAsync("/v1/architecture/request", JsonContent(request));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        var runId = created!.Run.RunId;

        var executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", content: null);
        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", content: null);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

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
        replayRunId.Should().NotBeNullOrWhiteSpace();

        var compareResponse = await Client.PostAsync(
            $"/v1/architecture/run/compare/end-to-end/summary?leftRunId={runId}&rightRunId={replayRunId}",
            JsonContent(new { persist = true }));

        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        compareResponse.Headers.TryGetValues("X-ArchiForge-ComparisonRecordId", out var ids).Should().BeTrue();

        var comparisonRecordId = ids!.Single();
        comparisonRecordId.Should().NotBeNullOrWhiteSpace();

        var recordResponse = await Client.GetAsync($"/v1/architecture/comparisons/{comparisonRecordId}");
        recordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await recordResponse.Content.ReadFromJsonAsync<ComparisonRecordResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Record.ComparisonType.Should().Be("end-to-end-replay");
        payload.Record.LeftRunId.Should().Be(runId);
        payload.Record.RightRunId.Should().Be(replayRunId);
        payload.Record.SummaryMarkdown.Should().NotBeNullOrWhiteSpace();
        payload.Record.PayloadJson.Should().NotBeNullOrWhiteSpace();
    }
}

