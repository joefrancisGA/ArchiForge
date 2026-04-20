using ArchLucid.Application.Value;
using ArchLucid.Core.Configuration;
using ArchLucid.Persistence.Value;

using FluentAssertions;

using Microsoft.Extensions.Options;

using ArchLucid.Contracts.ValueReports;

using Moq;

namespace ArchLucid.Application.Tests.Value;

public sealed class ValueReportBuilderTests
{
    [Fact]
    public async Task BuildAsync_maps_raw_metrics_and_roi_fields()
    {
        ValueReportRawMetrics raw = new(
            RunStatusCounts: [new ValueReportRunStatusCount("Completed", 3), new ValueReportRunStatusCount("Running", 1)],
            RunsCompletedCount: 2,
            ManifestsCommittedCount: 4,
            GovernanceEventCount: 10,
            DriftAlertEventCount: 8);

        Mock<IValueReportMetricsReader> reader = new();
        reader
            .Setup(r => r.ReadAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(raw);

        ValueReportComputationOptions options = new()
        {
            BaselineArchitectHoursBeforeArchLucidPerCommittedManifest = 8m,
            ArchitectHoursSavedFractionVsBaseline = 0.5m,
            GovernanceReviewHoursPerGovernanceEvent = 0.5m,
            DriftReviewHoursPerDriftAlertEvent = 0.25m,
            FullyLoadedArchitectHourlyUsd = 150m,
            BaselineAnnualSubscriptionAndOpsCostUsdFromRoiModel = 27360m,
            EstimatedLlmUsdPerCompletedRun = 5m,
        };

        Mock<IOptionsMonitor<ValueReportComputationOptions>> optMon = new();
        optMon.Setup(m => m.CurrentValue).Returns(options);

        ValueReportBuilder sut = new(reader.Object, optMon.Object);
        DateTimeOffset from = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        DateTimeOffset to = DateTimeOffset.Parse("2026-01-31T00:00:00Z");

        ValueReportSnapshot snap = await sut.BuildAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            from,
            to,
            CancellationToken.None);

        snap.ManifestsCommittedCount.Should().Be(4);
        snap.RunsCompletedCount.Should().Be(2);
        snap.GovernanceEventsHandledCount.Should().Be(10);
        snap.DriftAlertEventsCaughtCount.Should().Be(8);
        snap.EstimatedLlmCostForWindowUsd.Should().Be(10m);

        decimal expectedManifestHours = 4m * 8m * 0.5m;
        snap.EstimatedArchitectHoursSavedFromManifests.Should().Be(expectedManifestHours);
        snap.EstimatedArchitectHoursSavedFromGovernanceEvents.Should().Be(5m);
        snap.EstimatedArchitectHoursSavedFromDriftEvents.Should().Be(2m);
        snap.EstimatedTotalArchitectHoursSaved.Should().Be(expectedManifestHours + 5m + 2m);

        snap.RunStatusRows.Should().HaveCount(2);
    }
}
