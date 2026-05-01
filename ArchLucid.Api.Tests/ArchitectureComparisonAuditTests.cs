using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Tests.TestDtos;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture Comparison Audit.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ArchitectureComparisonAuditTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [SkippableFact]
    public async Task EndToEndComparisonSummary_PersistsComparisonRecord()
    {
        object request = TestRequestFactory.CreateArchitectureRequest("REQ-COMP-AUDIT-001");

        HttpResponseMessage createResponse = await Client.PostAsync("/v1/architecture/request", JsonContent(request));
        createResponse.EnsureSuccessStatusCode();

        CreateRunResponseDto? created =
            await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(JsonOptions);
        string runId = created!.Run.RunId;

        HttpResponseMessage executeResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage commitResponse = await Client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage replayResponse = await Client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(new { commitReplay = true, executionMode = "Current", manifestVersionOverride = "v1-replay" }));
        replayResponse.EnsureSuccessStatusCode();

        ReplayRunResponseDto? replayPayload =
            await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(JsonOptions);
        string replayRunId = replayPayload!.ReplayRunId;
        replayRunId.Should().NotBeNullOrWhiteSpace();

        HttpResponseMessage compareResponse = await Client.PostAsync(
            $"/v1/architecture/run/compare/end-to-end/summary?leftRunId={runId}&rightRunId={replayRunId}",
            JsonContent(new { persist = true }));

        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        compareResponse.Headers.TryGetValues("X-ArchLucid-ComparisonRecordId", out IEnumerable<string>? ids).Should()
            .BeTrue();

        string comparisonRecordId = ids!.Single();
        comparisonRecordId.Should().NotBeNullOrWhiteSpace();

        HttpResponseMessage recordResponse =
            await Client.GetAsync($"/v1/architecture/comparisons/{comparisonRecordId}");
        recordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ComparisonRecordResponseDto? payload =
            await recordResponse.Content.ReadFromJsonAsync<ComparisonRecordResponseDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Record.ComparisonType.Should().Be("end-to-end-replay");
        payload.Record.LeftRunId.Should().Be(runId);
        payload.Record.RightRunId.Should().Be(replayRunId);
        payload.Record.SummaryMarkdown.Should().NotBeNullOrWhiteSpace();
        payload.Record.PayloadJson.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class ReplayRunResponseDto
    {
        public string ReplayRunId
        {
            get;
            init;
        } = string.Empty;
    }
}
