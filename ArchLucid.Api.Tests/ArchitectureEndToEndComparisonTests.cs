using System.Net;
using System.Net.Http.Json;

using ArchLucid.Api.Models;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for Architecture End To End Comparison.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureEndToEndComparisonTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CompareRunsEndToEndSummary_ReturnsUnifiedSummary()
    {
        (string runId, string replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-E2E-001");

        string summaryUrl =
            $"/v1/architecture/run/compare/end-to-end/summary?leftRunId={Uri.EscapeDataString(runId)}&rightRunId={Uri.EscapeDataString(replayRunId)}";

        HttpResponseMessage response = await Client.PostAsync(
            summaryUrl,
            JsonContent(new PersistComparisonRequest { Persist = false }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        EndToEndReplayComparisonSummaryResponse? payload =
            await response.Content.ReadFromJsonAsync<EndToEndReplayComparisonSummaryResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Summary.Should().Contain("# End-to-End Replay Comparison:");
    }
}
