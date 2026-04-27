using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Tenancy;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Tenancy;

/// <summary>
///     Additional branch coverage for <see cref="InMemoryTenantRepository" /> (list, suspend, entra lookup, SCIM, no-op paths).
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryTenantRepositoryAdditionalCoverageTests
{
    [Fact]
    public async Task ListAsync_and_GetByEntra_round_trip()
    {
        InMemoryTenantRepository sut = new();
        Guid entra = Guid.NewGuid();
        Guid id = Guid.NewGuid();
        string slug = "l-" + Guid.NewGuid().ToString("N")[..8];
        await sut.InsertTenantAsync(id, "L", slug, TenantTier.Standard, entra, CancellationToken.None);

        (await sut.GetByEntraTenantIdAsync(entra, CancellationToken.None))!.Id.Should().Be(id);

        IReadOnlyList<TenantRecord> all = await sut.ListAsync(CancellationToken.None);
        all.Should().Contain(r => r.Id == id);
    }

    [Fact]
    public async Task SuspendTenant_async_sets_suspended_on_existing_row()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "S",
            "sus2-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);

        await sut.SuspendTenantAsync(id, CancellationToken.None);
        (await sut.GetByIdAsync(id, CancellationToken.None))!.SuspendedUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFirstWorkspaceAsync_returns_null_when_no_workspaces()
    {
        InMemoryTenantRepository sut = new();
        (await sut.GetFirstWorkspaceAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task CommitSelfService_and_UpdateBaseline_no_op_for_unknown_tenant()
    {
        InMemoryTenantRepository sut = new();
        Guid missing = Guid.NewGuid();
        await sut.CommitSelfServiceTrialAsync(
            missing,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            1,
            1,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);
        await sut.UpdateBaselineAsync(missing, 1m, 1, DateTimeOffset.UtcNow, CancellationToken.None);
        (await sut.GetByIdAsync(missing, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task MarkTrialConverted_updates_tier_when_trial_active()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "M",
            "mcv-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(2),
            3,
            2,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        await sut.MarkTrialConvertedAsync(id, TenantTier.Enterprise, CancellationToken.None);
        TenantRecord? r = await sut.GetByIdAsync(id, CancellationToken.None);
        r!.Tier.Should().Be(TenantTier.Enterprise);
        r.TrialStatus.Should().Be(TrialLifecycleStatus.Converted);
    }

    [Fact]
    public async Task TryMarkFirstManifestCommitted_second_invocation_returns_null()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "F",
            "fm-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            2,
            2,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        (await sut.TryMarkFirstManifestCommittedAsync(id, DateTimeOffset.UtcNow, CancellationToken.None))
            .Should()
            .NotBeNull();
        (await sut.TryMarkFirstManifestCommittedAsync(id, DateTimeOffset.UtcNow, CancellationToken.None))
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task TryRecordTrialLifecycleTransition_returns_false_when_status_mismatches()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "T",
            "trc-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            1,
            2,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        bool moved = await sut.TryRecordTrialLifecycleTransitionAsync(
            id,
            TrialLifecycleStatus.Converted,
            TrialLifecycleStatus.Expired,
            "x",
            CancellationToken.None);
        moved.Should().BeFalse();
    }

    [Fact]
    public async Task TryClaimTrialSeat_is_idempotent_for_same_principal()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "C",
            "clm-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            3,
            4,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);
        const string p = "dup@x.com";
        await sut.TryClaimTrialSeatAsync(id, p, CancellationToken.None);
        int used1 = (await sut.GetByIdAsync(id, CancellationToken.None))!.TrialSeatsUsed;
        await sut.TryClaimTrialSeatAsync(id, p, CancellationToken.None);
        int used2 = (await sut.GetByIdAsync(id, CancellationToken.None))!.TrialSeatsUsed;
        used2.Should().Be(used1);
    }

    [Fact]
    public async Task Enterprise_scim_increment_and_decrement()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "E",
            "sci-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Enterprise,
            null,
            CancellationToken.None,
            enterpriseScimSeatsLimit: 2);

        (await sut.TryIncrementEnterpriseScimSeatAsync(id, CancellationToken.None)).Should().BeTrue();
        (await sut.TryIncrementEnterpriseScimSeatAsync(id, CancellationToken.None)).Should().BeTrue();
        (await sut.TryIncrementEnterpriseScimSeatAsync(id, CancellationToken.None)).Should().BeFalse();
        await sut.DecrementEnterpriseScimSeatAsync(id, CancellationToken.None);
        (await sut.GetByIdAsync(id, CancellationToken.None))!.EnterpriseSeatsUsed.Should().Be(1);
    }

    [Fact]
    public async Task EnqueueTrialArchitecturePreseed_is_no_op_when_already_enqueued()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "Q",
            "q-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            1,
            2,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        await sut.EnqueueTrialArchitecturePreseedAsync(id, CancellationToken.None);
        DateTimeOffset? first = (await sut.GetByIdAsync(id, CancellationToken.None))!.TrialArchitecturePreseedEnqueuedUtc;
        await sut.EnqueueTrialArchitecturePreseedAsync(id, CancellationToken.None);
        (await sut.GetByIdAsync(id, CancellationToken.None))!.TrialArchitecturePreseedEnqueuedUtc.Should().Be(first);
    }
}
