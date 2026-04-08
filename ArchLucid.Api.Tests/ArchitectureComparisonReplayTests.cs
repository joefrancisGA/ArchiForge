using System.Net;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Tests for Architecture Comparison Replay.
/// </summary>

[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureComparisonReplayTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ReplayComparison_RecreatesPersistedEndToEndComparisonAsMarkdown()
    {
        (string runId, string replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-COMP-REPLAY-001");
        string comparisonRecordId = await ComparisonReplayTestFixture.PersistEndToEndComparisonAsync(
            Client, runId, replayRunId);

        HttpResponseMessage replayComparisonResponse = await Client.PostAsync(
            $"/v1/architecture/comparisons/{comparisonRecordId}/replay",
            JsonContent(new
            {
                format = "markdown"
            }));

        replayComparisonResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        replayComparisonResponse.Content.Headers.ContentType!.MediaType
            .Should().Be("text/markdown");

        string content = await replayComparisonResponse.Content.ReadAsStringAsync();
        content.Should().Contain("# ArchLucid End-to-End Replay Comparison Export");
    }
}

