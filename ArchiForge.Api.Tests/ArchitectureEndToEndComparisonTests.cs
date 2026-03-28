using System.Net;
using System.Net.Http.Json;

using ArchiForge.Api.Models;

using FluentAssertions;

namespace ArchiForge.Api.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ArchitectureEndToEndComparisonTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CompareRunsEndToEndSummary_ReturnsUnifiedSummary()
    {
        (string runId, string replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-E2E-001");

        HttpResponseMessage response = await Client.GetAsync(
            $"/v1/architecture/run/compare/end-to-end/summary?leftRunId={runId}&rightRunId={replayRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        EndToEndReplayComparisonSummaryResponse? payload = await response.Content.ReadFromJsonAsync<EndToEndReplayComparisonSummaryResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload.Summary.Should().Contain("# End-to-End Replay Comparison:");
    }
}

