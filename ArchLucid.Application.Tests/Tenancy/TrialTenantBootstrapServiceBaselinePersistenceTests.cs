using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Tenancy;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Tenancy;

public sealed class TrialTenantBootstrapServiceBaselinePersistenceTests
{
    [SkippableFact]
    public async Task CommitSelfServiceTrialAsync_round_trips_baseline_triple_via_in_memory_repo()
    {
        InMemoryTenantRepository repo = new();
        Guid tenantId = Guid.NewGuid();
        await repo.InsertTenantAsync(tenantId, "T", "t-" + tenantId.ToString("N")[..8], TenantTier.Free, null, CancellationToken.None);

        DateTimeOffset cap = DateTimeOffset.Parse("2026-04-10T10:00:00Z");

        await repo.CommitSelfServiceTrialAsync(
            tenantId,
            DateTimeOffset.Parse("2026-04-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-04-15T00:00:00Z"),
            runsLimit: 10,
            seatsLimit: 3,
            sampleRunId: Guid.NewGuid(),
            baselineReviewCycleHours: 24m,
            baselineReviewCycleSource: "ops estimate",
            baselineReviewCycleCapturedUtc: cap,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        TenantRecord? row = await repo.GetByIdAsync(tenantId, CancellationToken.None);

        row.Should().NotBeNull();
        row.BaselineReviewCycleHours.Should().Be(24m);
        row.BaselineReviewCycleSource.Should().Be("ops estimate");
        row.BaselineReviewCycleCapturedUtc.Should().Be(cap);
    }
}
