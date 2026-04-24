using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Tenancy;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Tenancy;

[Trait("Category", "Unit")]
[Trait("Suite", "Persistence")]
public sealed class InMemoryTenantRepositoryTrialFirstManifestCommittedTests
{
    [Fact]
    public async Task TryMarkTrialFirstManifestCommittedAsync_then_GetByIdAsync_returns_TrialFirstManifestCommittedUtc()
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
            CancellationToken.None);

        DateTimeOffset committed = DateTimeOffset.Parse("2026-04-10T15:30:00+00:00");
        TrialFirstManifestCommitOutcome? outcome =
            await sut.TryMarkTrialFirstManifestCommittedAsync(tenantId, committed, CancellationToken.None);

        outcome.Should().NotBeNull();

        TenantRecord? row = await sut.GetByIdAsync(tenantId, CancellationToken.None);

        row.Should().NotBeNull();
        row.TrialFirstManifestCommittedUtc.Should().Be(committed);
    }
}
