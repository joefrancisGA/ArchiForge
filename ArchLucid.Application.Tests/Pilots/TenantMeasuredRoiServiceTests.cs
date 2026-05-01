using ArchLucid.Application.Billing;
using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Pilots;

[Trait("Suite", "Core")]
public sealed class TenantMeasuredRoiServiceTests
{
    [SkippableFact]
    public async Task GetAsync_composes_snapshot_and_cost_and_disclaimer()
    {
        WhyArchLucidSnapshotResponse snap = new()
        {
            DemoRunId = "run-demo",
            RunsCreatedTotal = 3,
            AuditRowCount = 9,
        };

        Mock<IWhyArchLucidSnapshotService> snapshot = new();
        snapshot.Setup(s => s.BuildAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snap);

        TenantCostEstimate cost = new(
            "USD",
            TenantTier.Standard,
            10m,
            50m,
            ["tier"],
            "method");

        Mock<ITenantCostEstimateService> estimates = new();
        estimates.Setup(e => e.TryGetEstimateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cost);

        TenantMeasuredRoiService sut = new(snapshot.Object, estimates.Object);

        TenantMeasuredRoiSummary result = await sut.GetAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        result.ProcessSignals.Should().BeSameAs(snap);
        result.MonthlyCostBand.Should().Be(cost);
        result.Disclaimer.Should().Contain("cumulative");
        result.Disclaimer.Should().Contain("planning guidance");
    }

    [SkippableFact]
    public async Task GetAsync_free_tier_appends_free_tier_sentence()
    {
        Mock<IWhyArchLucidSnapshotService> snapshot = new();
        snapshot.Setup(s => s.BuildAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WhyArchLucidSnapshotResponse());

        TenantCostEstimate free = new("USD", TenantTier.Free, 0, 0, [], "m");

        Mock<ITenantCostEstimateService> estimates = new();
        estimates.Setup(e => e.TryGetEstimateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(free);

        TenantMeasuredRoiService sut = new(snapshot.Object, estimates.Object);

        TenantMeasuredRoiSummary result = await sut.GetAsync(Guid.NewGuid());

        result.Disclaimer.Should().Contain("Free tier");
    }
}
