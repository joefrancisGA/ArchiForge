using ArchLucid.Persistence.Pilots;

namespace ArchLucid.Persistence.Tests.Pilots;

[Trait("Category", "Unit")]
public sealed class NullPilotScorecardMetricsReaderTests
{
    [Fact]
    public async Task GetAsync_returns_zeroed_metrics_placeholder()
    {
        NullPilotScorecardMetricsReader reader = new();

        PilotScorecardTenantMetrics metrics =
            await reader.GetAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), CancellationToken.None);

        metrics.TotalRunsCommitted.Should().Be(0);
        metrics.TotalManifestsCreated.Should().Be(0);
        metrics.TotalFindingsResolved.Should().Be(0);
        metrics.AverageTimeToManifestMinutes.Should().BeNull();
        metrics.TotalAuditEventsGenerated.Should().Be(0);
        metrics.TotalGovernanceApprovalsCompleted.Should().Be(0);
        metrics.FirstCommitUtc.Should().BeNull();
    }
}
