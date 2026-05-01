using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Tenancy;

namespace ArchLucid.Persistence.Tests.Tenancy;

/// <summary>
///     Exercises <see cref="DapperTenantRepository" /> against a real catalog (Dapper, transactions, and UPDATE paths).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class DapperTenantRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [SkippableFact]
    public async Task GetBySlug_rejects_whitespace_before_sql()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);

        Func<Task> act = async () => await sut.GetBySlugAsync("   ", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [SkippableFact]
    public async Task Insert_get_by_id_slug_entra_list_and_workspace_round_trips()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        Guid entra = Guid.NewGuid();
        string slug = "ts-" + Guid.NewGuid().ToString("N")[..8];
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        await sut.InsertTenantAsync(
            tenantId,
            "SQL tenant A",
            slug,
            TenantTier.Free,
            entra,
            CancellationToken.None);

        TenantRecord? byId = await sut.GetByIdAsync(tenantId, CancellationToken.None);
        byId.Should().NotBeNull();
        byId.Slug.Should().Be(slug);
        byId.Tier.Should().Be(TenantTier.Free);
        byId.EntraTenantId.Should().Be(entra);

        TenantRecord? bySlug = await sut.GetBySlugAsync(slug, CancellationToken.None);
        bySlug!.Id.Should().Be(tenantId);

        (await sut.GetByEntraTenantIdAsync(entra, CancellationToken.None))!.Id.Should().Be(tenantId);

        IReadOnlyList<TenantRecord> list = await sut.ListAsync(CancellationToken.None);
        list.Select(static t => t.Id).Should().Contain(tenantId);

        await sut.InsertWorkspaceAsync(
            workspaceId,
            tenantId,
            "ws-1",
            projectId,
            CancellationToken.None);

        TenantWorkspaceLink? link = await sut.GetFirstWorkspaceAsync(tenantId, CancellationToken.None);
        link!.WorkspaceId.Should().Be(workspaceId);
        link.DefaultProjectId.Should().Be(projectId);
    }

    [SkippableFact]
    public async Task SuspendTenant_sets_suspended_utc()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        string slug = "sus-" + Guid.NewGuid().ToString("N")[..8];
        await sut.InsertTenantAsync(
            tenantId,
            "SQL suspend",
            slug,
            TenantTier.Standard,
            null,
            CancellationToken.None);

        await sut.SuspendTenantAsync(tenantId, CancellationToken.None);
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.SuspendedUtc.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Commit_trial_update_baseline_and_mark_converted()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        Guid sample = Guid.NewGuid();
        string slug = "tr-" + Guid.NewGuid().ToString("N")[..8];
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-1);
        DateTimeOffset exp = DateTimeOffset.UtcNow.AddDays(14);
        await sut.InsertTenantAsync(
            tenantId,
            "Trial T",
            slug,
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            tenantId,
            start,
            exp,
            runsLimit: 20,
            seatsLimit: 5,
            sample,
            10m,
            "src",
            start,
            "co",
            4,
            "ind",
            null,
            CancellationToken.None);
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.TrialStatus.Should().Be(TrialLifecycleStatus.Active);

        await sut.UpdateBaselineAsync(
            tenantId,
            2.5m,
            2,
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.BaselinePeoplePerReview.Should().Be(2);

        await sut.MarkTrialConvertedAsync(tenantId, TenantTier.Enterprise, CancellationToken.None);
        TenantRecord? r = await sut.GetByIdAsync(tenantId, CancellationToken.None);
        r!.Tier.Should().Be(TenantTier.Enterprise);
        r.TrialStatus.Should().Be(TrialLifecycleStatus.Converted);
    }

    [SkippableFact]
    public async Task List_automation_ids_preseed_pipeline_and_first_manifest_idempotent()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        Guid sample = Guid.NewGuid();
        string slug = "pre-" + Guid.NewGuid().ToString("N")[..8];
        await sut.InsertTenantAsync(
            tenantId,
            "Pre T",
            slug,
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            tenantId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            4,
            2,
            sample,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        (await sut.ListTrialLifecycleAutomationTenantIdsAsync(CancellationToken.None))
            .Should()
            .Contain(tenantId);

        await sut.EnqueueTrialArchitecturePreseedAsync(tenantId, CancellationToken.None);
        (await sut.ListTenantIdsPendingTrialArchitecturePreseedAsync(5, CancellationToken.None))
            .Should()
            .Contain(tenantId);

        Guid welcome = Guid.NewGuid();
        await sut.MarkTrialArchitecturePreseedCompletedAsync(tenantId, welcome, CancellationToken.None);
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.TrialWelcomeRunId.Should().Be(welcome);

        TrialFirstManifestCommitOutcome? first = await sut.TryMarkFirstManifestCommittedAsync(
            tenantId,
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        first.Should().NotBeNull();
        (await sut.TryMarkFirstManifestCommittedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None))
            .Should()
            .BeNull();
    }

    [SkippableFact]
    public async Task E2eHarnessSetTrialExpiresUtc_persists()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        string slug = "e2e-" + Guid.NewGuid().ToString("N")[..8];
        await sut.InsertTenantAsync(
            tenantId,
            "E2E",
            slug,
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            tenantId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(3),
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
        DateTimeOffset next = DateTimeOffset.UtcNow.AddDays(60);
        await sut.E2eHarnessSetTrialExpiresUtcAsync(tenantId, next, CancellationToken.None);
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.TrialExpiresUtc.Should().BeCloseTo(next, TimeSpan.FromSeconds(1));
    }

    [SkippableFact]
    public async Task TryIncrementActiveTrialRun_increments_until_cap()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        string slug = "run-" + Guid.NewGuid().ToString("N")[..8];
        await sut.InsertTenantAsync(
            tenantId,
            "Run cap",
            slug,
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            tenantId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            runsLimit: 2,
            seatsLimit: 3,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);
        await sut.TryIncrementActiveTrialRunAsync(tenantId, CancellationToken.None);
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.TrialRunsUsed.Should().Be(1);
        await sut.TryIncrementActiveTrialRunAsync(tenantId, CancellationToken.None);
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.TrialRunsUsed.Should().Be(2);
        await Assert.ThrowsAsync<TrialLimitExceededException>(
            async () => await sut.TryIncrementActiveTrialRunAsync(tenantId, CancellationToken.None));
    }

    [SkippableFact]
    public async Task TryClaimTrialSeat_respects_duplicate_and_seat_limit()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        string slug = "seat-" + Guid.NewGuid().ToString("N")[..8];
        await sut.InsertTenantAsync(
            tenantId,
            "Seats",
            slug,
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            tenantId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            10,
            seatsLimit: 2,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);
        const string p1 = "p1@contoso.com";
        const string p2 = "p2@contoso.com";
        await sut.TryClaimTrialSeatAsync(tenantId, p1, CancellationToken.None);
        await sut.TryClaimTrialSeatAsync(tenantId, p1, CancellationToken.None);
        await sut.TryClaimTrialSeatAsync(tenantId, p2, CancellationToken.None);
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.TrialSeatsUsed.Should().Be(2);
        await Assert.ThrowsAsync<TrialLimitExceededException>(
            async () => await sut.TryClaimTrialSeatAsync(tenantId, "p3@contoso.com", CancellationToken.None));
    }

    [SkippableFact]
    public async Task TryRecordTrialLifecycleTransition_succeeds_and_fails_on_mismatch()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        string slug = "tlc-" + Guid.NewGuid().ToString("N")[..8];
        await sut.InsertTenantAsync(
            tenantId,
            "Tlc",
            slug,
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            tenantId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1),
            5,
            3,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        bool ok = await sut.TryRecordTrialLifecycleTransitionAsync(
            tenantId,
            TrialLifecycleStatus.Active,
            TrialLifecycleStatus.Expired,
            "unit test",
            CancellationToken.None);
        ok.Should().BeTrue();
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.TrialStatus.Should().Be(TrialLifecycleStatus.Expired);

        bool bad = await sut.TryRecordTrialLifecycleTransitionAsync(
            tenantId,
            TrialLifecycleStatus.Active,
            "x",
            "nope",
            CancellationToken.None);
        bad.Should().BeFalse();
    }

    [SkippableFact]
    public async Task Enterprise_scim_seat_bump_respects_limit_and_decrements()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        string slug = "ent-" + Guid.NewGuid().ToString("N")[..8];
        await sut.InsertTenantAsync(
            tenantId,
            "Ent",
            slug,
            TenantTier.Enterprise,
            null,
            CancellationToken.None,
            enterpriseScimSeatsLimit: 1);

        (await sut.TryIncrementEnterpriseScimSeatAsync(tenantId, CancellationToken.None)).Should().BeTrue();
        (await sut.TryIncrementEnterpriseScimSeatAsync(tenantId, CancellationToken.None)).Should().BeFalse();
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.EnterpriseSeatsUsed.Should().Be(1);
        await sut.DecrementEnterpriseScimSeatAsync(tenantId, CancellationToken.None);
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.EnterpriseSeatsUsed.Should().Be(0);
    }

    [SkippableFact]
    public async Task UpdateEntraTenantIdAsync_binds_after_trial_convert_and_is_idempotent()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        Guid corpEntra = Guid.NewGuid();
        string slug = "hand-" + Guid.NewGuid().ToString("N")[..8];
        await sut.InsertTenantAsync(
            tenantId,
            "Handoff T",
            slug,
            TenantTier.Standard,
            null,
            CancellationToken.None);
        await sut.CommitSelfServiceTrialAsync(
            tenantId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(3),
            5,
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
        await sut.MarkTrialConvertedAsync(tenantId, TenantTier.Standard, CancellationToken.None);
        TenantRecord? converted = await sut.GetByIdAsync(tenantId, CancellationToken.None);
        converted!.TrialStatus.Should().Be(TrialLifecycleStatus.Converted);
        converted.EntraTenantId.Should().BeNull();

        (await sut.UpdateEntraTenantIdAsync(tenantId, corpEntra, CancellationToken.None)).Should().BeTrue();
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.EntraTenantId.Should().Be(corpEntra);

        (await sut.UpdateEntraTenantIdAsync(tenantId, corpEntra, CancellationToken.None)).Should().BeTrue();
    }

    [SkippableFact]
    public async Task UpdateEntraTenantIdAsync_noop_when_row_has_different_entra()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);

        TestSqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperTenantRepository sut = new(factory);
        Guid tenantId = Guid.NewGuid();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        string slug = "lock-" + Guid.NewGuid().ToString("N")[..8];
        await sut.InsertTenantAsync(
            tenantId,
            "Locked",
            slug,
            TenantTier.Standard,
            first,
            CancellationToken.None);

        (await sut.UpdateEntraTenantIdAsync(tenantId, second, CancellationToken.None)).Should().BeFalse();
        (await sut.GetByIdAsync(tenantId, CancellationToken.None))!.EntraTenantId.Should().Be(first);
    }
}
