using System.Net;
using System.Net.Http.Json;
using ArchiForge.Api.Models;
using FluentAssertions;
using Xunit;

namespace ArchiForge.Api.Tests;

public sealed class ArchitectureEndToEndComparisonTests : IntegrationTestBase
{
    public ArchitectureEndToEndComparisonTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CompareRunsEndToEndSummary_ReturnsUnifiedSummary()
    {
        var (runId, replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-E2E-001");

        var response = await Client.GetAsync(
            $"/v1/architecture/run/compare/end-to-end/summary?leftRunId={runId}&rightRunId={replayRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EndToEndReplayComparisonSummaryResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Summary.Should().Contain("# End-to-End Replay Comparison:");
    }
}

