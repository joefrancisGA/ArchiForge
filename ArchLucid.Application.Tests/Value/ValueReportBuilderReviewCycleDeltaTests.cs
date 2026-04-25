using ArchLucid.Application.Value;
using ArchLucid.Contracts.ValueReports;
using ArchLucid.Core.Configuration;
using ArchLucid.Persistence.Value;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Value;

public sealed class ValueReportBuilderReviewCycleDeltaTests
{
    [Fact]
    public async Task BuildAsync_NoMeasurementYet_when_no_committed_manifests_in_window()
    {
        ValueReportRawMetrics raw = new(
            [],
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            null,
            null,
            MeasuredAverageReviewCycleHoursForWindow: null,
            MeasuredReviewCycleSampleSize: 0,
            null,
            null,
            null);

        ValueReportSnapshot snap = await BuildSnapshotAsync(raw);

        snap.ReviewCycleBaselineProvenance.Should().Be(ReviewCycleBaselineProvenance.NoMeasurementYet);
        snap.ReviewCycleHoursDelta.Should().BeNull();
        snap.ReviewCycleHoursDeltaPercent.Should().BeNull();
    }

    [Fact]
    public async Task BuildAsync_TenantSupplied_provenance_and_deltas()
    {
        ValueReportRawMetrics raw = new(
            [],
            0,
            0,
            0,
            0,
            0,
            0,
            TenantBaselineReviewCycleHours: 20m,
            TenantBaselineReviewCycleSource: "estimate",
            TenantBaselineReviewCycleCapturedUtc: DateTimeOffset.Parse("2026-04-01T00:00:00Z"),
            MeasuredAverageReviewCycleHoursForWindow: 12m,
            MeasuredReviewCycleSampleSize: 3,
            null,
            null,
            null);

        ValueReportSnapshot snap = await BuildSnapshotAsync(raw);

        snap.ReviewCycleBaselineProvenance.Should().Be(ReviewCycleBaselineProvenance.TenantSuppliedAtSignup);
        snap.ReviewCycleHoursDelta.Should().Be(8m);
        snap.ReviewCycleHoursDeltaPercent.Should().BeApproximately(40m, 0.0001m);
    }

    [Fact]
    public async Task BuildAsync_DefaultedFromRoiModel_when_tenant_baseline_null_but_measured_present()
    {
        ValueReportRawMetrics raw = new(
            [],
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            null,
            null,
            MeasuredAverageReviewCycleHoursForWindow: 4m,
            MeasuredReviewCycleSampleSize: 2,
            null,
            null,
            null);

        ValueReportSnapshot snap = await BuildSnapshotAsync(raw);

        snap.ReviewCycleBaselineProvenance.Should().Be(ReviewCycleBaselineProvenance.DefaultedFromRoiModelOptions);
        snap.ReviewCycleHoursDelta.Should().Be(4m);
        snap.ReviewCycleHoursDeltaPercent.Should().Be(50m);
    }

    [Fact]
    public async Task BuildAsync_DeltaPercent_null_when_effective_baseline_is_zero()
    {
        ValueReportRawMetrics raw = new(
            [],
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            null,
            null,
            MeasuredAverageReviewCycleHoursForWindow: 1m,
            MeasuredReviewCycleSampleSize: 1,
            null,
            null,
            null);

        Mock<IOptionsMonitor<ValueReportComputationOptions>> opt = new();
        opt.Setup(o => o.CurrentValue).Returns(
            new ValueReportComputationOptions { BaselineArchitectHoursBeforeArchLucidPerCommittedManifest = 0m });

        Mock<IValueReportMetricsReader> reader = new();
        reader.Setup(r => r.ReadAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(raw);

        ValueReportBuilder sut = new(reader.Object, opt.Object);
        ValueReportSnapshot snap = await sut.BuildAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-31T00:00:00Z"),
            CancellationToken.None);

        snap.ReviewCycleBaselineProvenance.Should().Be(ReviewCycleBaselineProvenance.DefaultedFromRoiModelOptions);
        snap.ReviewCycleHoursDelta.Should().Be(-1m);
        snap.ReviewCycleHoursDeltaPercent.Should().BeNull();
    }

    private static async Task<ValueReportSnapshot> BuildSnapshotAsync(ValueReportRawMetrics raw)
    {
        Mock<IValueReportMetricsReader> reader = new();
        reader.Setup(r => r.ReadAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(raw);

        Mock<IOptionsMonitor<ValueReportComputationOptions>> opt = new();
        opt.Setup(o => o.CurrentValue).Returns(new ValueReportComputationOptions { BaselineArchitectHoursBeforeArchLucidPerCommittedManifest = 8m });

        ValueReportBuilder sut = new(reader.Object, opt.Object);

        return await sut.BuildAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-31T00:00:00Z"),
            CancellationToken.None);
    }
}
