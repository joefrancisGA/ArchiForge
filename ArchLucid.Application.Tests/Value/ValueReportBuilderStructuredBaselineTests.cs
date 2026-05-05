using ArchLucid.Application.Value;
using ArchLucid.Contracts.ValueReports;
using ArchLucid.Core.Configuration;
using ArchLucid.Persistence.Value;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Value;

[Trait("Suite", "Core")]
[Trait("Category", "ValueReportBuilderStructuredBaseline")]
public sealed class ValueReportBuilderStructuredBaselineTests
{
    [SkippableFact]
    public async Task BuildAsync_uses_tenant_manual_prep_for_manifest_hours_when_set()
    {
        ValueReportRawMetrics raw = new(
            [],
            1,
            2,
            0,
            0,
            0,
            0,
            null,
            null,
            null,
            4m,
            1,
            3m,
            null,
            5);

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
        opt.Setup(o => o.CurrentValue).Returns(
            new ValueReportComputationOptions
            {
                BaselineArchitectHoursBeforeArchLucidPerCommittedManifest = 8m,
                ArchitectHoursSavedFractionVsBaseline = 0.5m,
                FullyLoadedArchitectHourlyUsd = 100m
            });

        ValueReportBuilder sut = new(reader.Object, opt.Object);
        ValueReportSnapshot snap = await sut.BuildAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-08T00:00:00Z"),
            CancellationToken.None);

        snap.TenantBaselineManualPrepHoursPerReview.Should().Be(3m);
        // 2 manifests * 3h * 0.5
        snap.EstimatedArchitectHoursSavedFromManifests.Should().Be(3m);
    }

    [SkippableFact]
    public async Task BuildAsync_falls_back_to_model_constant_when_tenant_manual_prep_null()
    {
        ValueReportRawMetrics raw = new(
            [],
            1,
            2,
            0,
            0,
            0,
            0,
            null,
            null,
            null,
            null,
            0,
            null,
            null,
            4);

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
        opt.Setup(o => o.CurrentValue).Returns(
            new ValueReportComputationOptions { BaselineArchitectHoursBeforeArchLucidPerCommittedManifest = 8m, ArchitectHoursSavedFractionVsBaseline = 0.5m });

        ValueReportBuilder sut = new(reader.Object, opt.Object);
        ValueReportSnapshot snap = await sut.BuildAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-02T00:00:00Z"),
            CancellationToken.None);

        snap.TenantBaselineManualPrepHoursPerReview.Should().BeNull();
        // 2 * 8 * 0.5
        snap.EstimatedArchitectHoursSavedFromManifests.Should().Be(8m);
    }

    [SkippableFact]
    public async Task BuildAsync_scales_hourly_value_when_people_per_review_set()
    {
        ValueReportRawMetrics raw = new(
            [],
            1,
            1,
            0,
            0,
            0,
            0,
            null,
            null,
            null,
            null,
            0,
            null,
            8,
            2);

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
        opt.Setup(o => o.CurrentValue).Returns(
            new ValueReportComputationOptions
            {
                BaselineArchitectHoursBeforeArchLucidPerCommittedManifest = 1m,
                ArchitectHoursSavedFractionVsBaseline = 1m,
                FullyLoadedArchitectHourlyUsd = 100m,
                DefaultTeamSizeForHourlyCostScaling = 2m
            });

        ValueReportBuilder sut = new(reader.Object, opt.Object);
        ValueReportSnapshot snap = await sut.BuildAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-02T00:00:00Z"),
            CancellationToken.None);

        // totalHours = 1 manifest * 1h/ manifest * 1; hourly = 100 * (8 people / 2 team size) = 400; annualized
        snap.AnnualizedHoursValueUsd.Should().Be(400m * 365m);
    }
}
