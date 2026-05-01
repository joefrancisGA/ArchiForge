using ArchLucid.Persistence.Value;

namespace ArchLucid.Persistence.Tests.Value;

public sealed class InMemoryValueReportMetricsReaderTests
{
    [SkippableFact]
    public async Task ReadAsync_returns_empty_metrics_for_any_scope()
    {
        InMemoryValueReportMetricsReader sut = new();
        DateTimeOffset from = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        DateTimeOffset to = DateTimeOffset.Parse("2026-02-01T00:00:00Z");

        ValueReportRawMetrics raw = await sut.ReadAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            from,
            to,
            CancellationToken.None);

        raw.RunsCompletedCount.Should().Be(0);
        raw.ManifestsCommittedCount.Should().Be(0);
        raw.GovernanceEventCount.Should().Be(0);
        raw.DriftAlertEventCount.Should().Be(0);
        raw.RunStatusCounts.Should().BeEmpty();
    }
}
