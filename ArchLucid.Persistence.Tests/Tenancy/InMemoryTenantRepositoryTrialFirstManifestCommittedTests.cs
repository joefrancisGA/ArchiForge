using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Tenancy;

namespace ArchLucid.Persistence.Tests.Tenancy;

[Trait("Category", "Unit")]
[Trait("Suite", "Persistence")]
public sealed class InMemoryTenantRepositoryTrialFirstManifestCommittedTests
{
    [Fact]
    public async Task TryMarkFirstManifestCommittedAsync_then_GetByIdAsync_returns_TrialFirstManifestCommittedUtc_for_trial_tenant()
    {
        Guid tenantId = Guid.NewGuid();
        InMemoryTenantRepository sut = new();

        await sut.InsertTenantAsync(tenantId, "Acme", "acme", TenantTier.Free, null, CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            tenantId,
            DateTimeOffset.Parse("2026-04-01T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-05-01T00:00:00+00:00"),
            10,
            5,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        DateTimeOffset committed = DateTimeOffset.Parse("2026-04-10T15:30:00+00:00");
        TrialFirstManifestCommitOutcome? outcome =
            await sut.TryMarkFirstManifestCommittedAsync(tenantId, committed, CancellationToken.None);

        outcome.Should().NotBeNull();

        TenantRecord? row = await sut.GetByIdAsync(tenantId, CancellationToken.None);

        row.Should().NotBeNull();
        row!.TrialFirstManifestCommittedUtc.Should().Be(committed);
    }

    [Fact]
    public async Task TryMarkFirstManifestCommittedAsync_sets_anchor_for_non_trial_tenant()
    {
        Guid tenantId = Guid.NewGuid();
        InMemoryTenantRepository sut = new();

        await sut.InsertTenantAsync(tenantId, "Contoso", "contoso", TenantTier.Enterprise, null, CancellationToken.None);

        DateTimeOffset committed = DateTimeOffset.Parse("2026-06-01T08:00:00+00:00");
        TrialFirstManifestCommitOutcome? outcome =
            await sut.TryMarkFirstManifestCommittedAsync(tenantId, committed, CancellationToken.None);

        outcome.Should().NotBeNull();

        TenantRecord? row = await sut.GetByIdAsync(tenantId, CancellationToken.None);

        row.Should().NotBeNull();
        row!.TrialFirstManifestCommittedUtc.Should().Be(committed);
        row.TrialExpiresUtc.Should().BeNull();
    }
}
