using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Tenancy;

namespace ArchLucid.Persistence.Tests.Tenancy;

/// <summary>
///     Exercises <see cref="InMemoryTenantRepository" /> method bodies not fully covered by contract tests
///     (happy paths, guard clauses, and lock-protected trial transitions).
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryTenantRepositoryCoverageTests
{
    [SkippableFact]
    public async Task Full_trial_lifecycle_including_preseed_list_and_first_manifest_enqueues_and_completes()
    {
        InMemoryTenantRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        Guid sampleRun = Guid.NewGuid();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-1);
        DateTimeOffset exp = DateTimeOffset.UtcNow.AddDays(7);

        await sut.InsertTenantAsync(
            tenantId,
            "T",
            "t-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);

        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.Tier.Should().Be(TenantTier.Standard);

        await sut.CommitSelfServiceTrialAsync(
            tenantId,
            start,
            exp,
            runsLimit: 3,
            seatsLimit: 2,
            sampleRunId: sampleRun,
            baselineReviewCycleHours: 24,
            baselineReviewCycleSource: "s",
            baselineReviewCycleCapturedUtc: start,
            companySize: "S",
            architectureTeamSize: 2,
            industryVertical: "gov",
            industryVerticalOther: null,
            CancellationToken.None);

        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.TrialStatus.Should().Be(TrialLifecycleStatus.Active);

        await sut.UpdateBaselineAsync(
            tenantId,
            manualPrepHoursPerReview: 1.5m,
            peoplePerReview: 3,
            capturedUtc: DateTimeOffset.UtcNow,
            CancellationToken.None);

        await sut.InsertWorkspaceAsync(
            Guid.NewGuid(),
            tenantId,
            "ws",
            defaultProjectId: Guid.NewGuid(),
            CancellationToken.None);

        (await sut.GetFirstWorkspaceAsync(tenantId, CancellationToken.None))!.WorkspaceId.Should().NotBeEmpty();

        IReadOnlyList<Guid> auto = await sut.ListTrialLifecycleAutomationTenantIdsAsync(CancellationToken.None);
        auto.Should().Contain(tenantId);

        await sut.EnqueueTrialArchitecturePreseedAsync(tenantId, CancellationToken.None);
        (await sut.ListTenantIdsPendingTrialArchitecturePreseedAsync(10, CancellationToken.None)).Should()
            .Contain(tenantId);

        await sut.MarkTrialArchitecturePreseedCompletedAsync(
            tenantId,
            welcomeRunId: Guid.NewGuid(),
            CancellationToken.None);

        TrialFirstManifestCommitOutcome? o = await sut.TryMarkFirstManifestCommittedAsync(
            tenantId,
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        o.Should().NotBeNull();
        o.TrialRunUsageRatio.Should().BeGreaterThanOrEqualTo(0);

        await sut.E2eHarnessSetTrialExpiresUtcAsync(
            tenantId,
            DateTimeOffset.UtcNow.AddDays(30),
            CancellationToken.None);

        bool transitioned = await sut.TryRecordTrialLifecycleTransitionAsync(
            tenantId,
            expectedCurrentStatus: TrialLifecycleStatus.Active,
            nextStatus: "Pausing",
            reason: "x",
            CancellationToken.None);
        transitioned.Should().BeTrue();
    }

    [SkippableFact]
    public async Task InsertTenant_throws_on_duplicate_id_slug_or_entra()
    {
        InMemoryTenantRepository sut = new();
        Guid t1 = Guid.NewGuid();
        string slug = "slug-" + Guid.NewGuid().ToString("N")[..8];
        Guid entra = Guid.NewGuid();

        await sut.InsertTenantAsync(
            t1,
            "A",
            slug,
            TenantTier.Standard,
            entra,
            CancellationToken.None);

        Func<Task> dupId = async () => await sut.InsertTenantAsync(
            t1,
            "B",
            "other-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await dupId.Should().ThrowAsync<InvalidOperationException>();

        Guid t2 = Guid.NewGuid();
        Func<Task> dupSlug = async () => await sut.InsertTenantAsync(
            t2,
            "B",
            slug,
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await dupSlug.Should().ThrowAsync<InvalidOperationException>();

        Guid t3 = Guid.NewGuid();
        Func<Task> dupEntra = async () => await sut.InsertTenantAsync(
            t3,
            "C",
            "z-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            entra,
            CancellationToken.None);
        await dupEntra.Should().ThrowAsync<InvalidOperationException>();
    }

    [SkippableFact]
    public async Task GetBySlug_Throws_WhenNullOrEmpty()
    {
        InMemoryTenantRepository sut = new();

        Func<Task> a = async () => await sut.GetBySlugAsync(" ", CancellationToken.None);
        await a.Should().ThrowAsync<ArgumentException>();
    }

    [SkippableFact]
    public async Task MarkTrialConverted_skips_When_trial_not_active()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "N",
            "m-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);

        await sut.MarkTrialConvertedAsync(id, TenantTier.Enterprise, CancellationToken.None);
        (await sut.GetByIdAsync(id, CancellationToken.None))!.Tier.Should().Be(TenantTier.Standard);
    }

    [SkippableFact]
    public async Task TryIncrementActiveTrialRun_throws_When_trial_expired()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "E",
            "e-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            id,
            DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow.AddSeconds(-1),
            runsLimit: 1,
            seatsLimit: 2,
            sampleRunId: Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        await Assert.ThrowsAsync<TrialLimitExceededException>(
            async () => await sut.TryIncrementActiveTrialRunAsync(id, CancellationToken.None));
    }

    [SkippableFact]
    public async Task TryIncrementActiveTrialRun_throws_When_runs_exceeded()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "E3",
            "e3-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            runsLimit: 1,
            seatsLimit: 2,
            sampleRunId: Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);
        await sut.TryIncrementActiveTrialRunAsync(id, CancellationToken.None);
        await Assert.ThrowsAsync<TrialLimitExceededException>(
            async () => await sut.TryIncrementActiveTrialRunAsync(id, CancellationToken.None));
    }

    [SkippableFact]
    public async Task TryClaimTrialSeat_throws_WhenSeatsOrTrial_expired()
    {
        InMemoryTenantRepository sut = new();
        Guid id = Guid.NewGuid();
        await sut.InsertTenantAsync(
            id,
            "S",
            "s-" + Guid.NewGuid().ToString("N")[..8],
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddSeconds(-1),
            runsLimit: 5,
            seatsLimit: 2,
            sampleRunId: Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        await Assert.ThrowsAsync<TrialLimitExceededException>(
            async () => await sut.TryClaimTrialSeatAsync(id, "a@b", CancellationToken.None));
    }
}
