using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Tenancy;

namespace ArchLucid.Persistence.Tests.Tenancy;

[Trait("Suite", "Core")]
public sealed class InMemoryTenantRepositoryEnterpriseScimSeatTests
{
    [SkippableFact]
    public async Task TryIncrementEnterpriseScimSeat_respects_limit_then_decrement_frees()
    {
        Guid tenantId = Guid.NewGuid();
        InMemoryTenantRepository sut = new();
        await sut.InsertTenantAsync(
            tenantId,
            "Scim Seat Tenant",
            "slug-scim-seat",
            TenantTier.Enterprise,
            null,
            default,
            2);

        (await sut.TryIncrementEnterpriseScimSeatAsync(tenantId, default)).Should().BeTrue();
        (await sut.TryIncrementEnterpriseScimSeatAsync(tenantId, default)).Should().BeTrue();
        (await sut.TryIncrementEnterpriseScimSeatAsync(tenantId, default)).Should().BeFalse();

        await sut.DecrementEnterpriseScimSeatAsync(tenantId, default);
        (await sut.TryIncrementEnterpriseScimSeatAsync(tenantId, default)).Should().BeTrue();
    }

    [SkippableFact]
    public async Task TryIncrementEnterpriseScimSeat_unlimited_when_limit_null()
    {
        Guid tenantId = Guid.NewGuid();
        InMemoryTenantRepository sut = new();
        await sut.InsertTenantAsync(tenantId, "Unlim", "slug-unlim", TenantTier.Free, null, default);

        for (int i = 0; i < 5; i++)
            (await sut.TryIncrementEnterpriseScimSeatAsync(tenantId, default)).Should().BeTrue();
    }
}
