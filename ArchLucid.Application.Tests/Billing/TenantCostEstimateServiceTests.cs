using ArchLucid.Application.Billing;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Moq;

namespace ArchLucid.Application.Tests.Billing;

public sealed class TenantCostEstimateServiceTests
{
    [Fact]
    public async Task TryGetEstimateAsync_missing_tenant_returns_null()
    {
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantRecord?)null);

        TenantCostEstimateService sut = new(
            tenants.Object,
            new BillingOptionsTestMonitor<BillingUnitRatesOptions>(new BillingUnitRatesOptions()));

        TenantCostEstimate? result = await sut.TryGetEstimateAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetEstimateAsync_standard_tenant_returns_band()
    {
        Mock<ITenantRepository> tenants = new();
        tenants.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantRecord { Tier = TenantTier.Standard });

        BillingUnitRatesOptions rates = new()
        {
            Currency = "USD",
            StandardMonthlyUsdLow = 10,
            StandardMonthlyUsdHigh = 20,
        };

        TenantCostEstimateService sut = new(tenants.Object, new BillingOptionsTestMonitor<BillingUnitRatesOptions>(rates));

        TenantCostEstimate? result = await sut.TryGetEstimateAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.EstimatedMonthlyUsdLow.Should().Be(10);
        result.EstimatedMonthlyUsdHigh.Should().Be(20);
        result.Tier.Should().Be(TenantTier.Standard);
    }
}
