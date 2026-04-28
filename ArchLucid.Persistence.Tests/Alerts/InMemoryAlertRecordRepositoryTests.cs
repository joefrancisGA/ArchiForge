using ArchLucid.Core.Pagination;
using ArchLucid.Decisioning.Alerts;

namespace ArchLucid.Persistence.Tests.Alerts;

[Trait("Category", "Unit")]
[Trait("Suite", "Persistence")]
public sealed class InMemoryAlertRecordRepositoryTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly DateTime BaseUtc = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_returns_same_row()
    {
        InMemoryAlertRecordRepository repo = new();
        Guid alertId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        AlertRecord alert = BuildAlert(alertId, AlertStatus.Open, BaseUtc, "k1");

        await repo.CreateAsync(alert, CancellationToken.None);

        AlertRecord? loaded = await repo.GetByIdAsync(alertId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.AlertId.Should().Be(alertId);
        loaded.DeduplicationKey.Should().Be("k1");
    }

    [Fact]
    public async Task UpdateAsync_replaces_existing_alert_by_AlertId()
    {
        InMemoryAlertRecordRepository repo = new();
        Guid alertId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        AlertRecord original = BuildAlert(alertId, AlertStatus.Open, BaseUtc, "k");
        await repo.CreateAsync(original, CancellationToken.None);

        AlertRecord updated = BuildAlert(alertId, AlertStatus.Resolved, BaseUtc.AddHours(1), "k");
        updated.Title = "updated";

        await repo.UpdateAsync(updated, CancellationToken.None);

        AlertRecord? loaded = await repo.GetByIdAsync(alertId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(AlertStatus.Resolved);
        loaded.Title.Should().Be("updated");
    }

    [Fact]
    public async Task CreateAsync_trims_oldest_when_exceeding_MaxEntries_500()
    {
        InMemoryAlertRecordRepository repo = new();
        Guid firstId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        AlertRecord first = BuildAlert(firstId, AlertStatus.Open, BaseUtc, "first");
        await repo.CreateAsync(first, CancellationToken.None);

        Task[] tail = Enumerable
            .Range(2, 500)
            .Select(i => repo.CreateAsync(
                BuildAlert(
                    Guid.Parse($"10000000-0000-0000-0000-{i:000000000000}"),
                    AlertStatus.Open,
                    BaseUtc.AddMinutes(i),
                    $"k{i}"),
                CancellationToken.None))
            .ToArray();

        await Task.WhenAll(tail);

        AlertRecord? gone = await repo.GetByIdAsync(firstId, CancellationToken.None);
        gone.Should().BeNull();

        IReadOnlyList<AlertRecord> scope =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, null, 600, CancellationToken.None);
        scope.Should().HaveCount(500);
    }

    [Fact]
    public async Task GetOpenByDeduplicationKeyAsync_returns_newest_Open_or_Acknowledged_in_scope_only()
    {
        InMemoryAlertRecordRepository repo = new();
        string key = "dedupe-a";

        await repo.CreateAsync(
            BuildAlert(Guid.Parse("20000000-0000-0000-0000-000000000001"), AlertStatus.Open, BaseUtc, key),
            CancellationToken.None);

        await repo.CreateAsync(
            BuildAlert(Guid.Parse("20000000-0000-0000-0000-000000000002"), AlertStatus.Acknowledged,
                BaseUtc.AddHours(1), key),
            CancellationToken.None);

        await repo.CreateAsync(
            BuildAlert(Guid.Parse("20000000-0000-0000-0000-000000000003"), AlertStatus.Open, BaseUtc.AddHours(2), key),
            CancellationToken.None);

        AlertRecord? match =
            await repo.GetOpenByDeduplicationKeyAsync(TenantId, WorkspaceId, ProjectId, key, CancellationToken.None);
        match.Should().NotBeNull();
        match.AlertId.Should().Be(Guid.Parse("20000000-0000-0000-0000-000000000003"));
    }

    [Fact]
    public async Task GetOpenByDeduplicationKeyAsync_ignores_Resolved_and_wrong_scope()
    {
        InMemoryAlertRecordRepository repo = new();
        string key = "dedupe-b";

        await repo.CreateAsync(
            BuildAlert(Guid.Parse("21000000-0000-0000-0000-000000000001"), AlertStatus.Resolved, BaseUtc, key),
            CancellationToken.None);

        AlertRecord? noResolved =
            await repo.GetOpenByDeduplicationKeyAsync(TenantId, WorkspaceId, ProjectId, key, CancellationToken.None);
        noResolved.Should().BeNull();

        await repo.CreateAsync(
            BuildAlert(
                Guid.Parse("21000000-0000-0000-0000-000000000002"),
                AlertStatus.Open,
                BaseUtc,
                key,
                Guid.Parse("99999999-9999-9999-9999-999999999999")),
            CancellationToken.None);

        AlertRecord? noOtherTenant =
            await repo.GetOpenByDeduplicationKeyAsync(TenantId, WorkspaceId, ProjectId, key, CancellationToken.None);
        noOtherTenant.Should().BeNull();
    }

    [Fact]
    public async Task ListByScopeAsync_orders_newest_CreatedUtc_first_and_clamps_take()
    {
        InMemoryAlertRecordRepository repo = new();

        await repo.CreateAsync(
            BuildAlert(Guid.Parse("30000000-0000-0000-0000-000000000001"), AlertStatus.Open, BaseUtc, "x"),
            CancellationToken.None);

        await repo.CreateAsync(
            BuildAlert(Guid.Parse("30000000-0000-0000-0000-000000000002"), AlertStatus.Open, BaseUtc.AddHours(3), "x"),
            CancellationToken.None);

        await repo.CreateAsync(
            BuildAlert(Guid.Parse("30000000-0000-0000-0000-000000000003"), AlertStatus.Open, BaseUtc.AddHours(1), "x"),
            CancellationToken.None);

        IReadOnlyList<AlertRecord> list =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, null, 2, CancellationToken.None);
        list.Should().HaveCount(2);
        list[0].CreatedUtc.Should().Be(BaseUtc.AddHours(3));
        list[1].CreatedUtc.Should().Be(BaseUtc.AddHours(1));

        IReadOnlyList<AlertRecord> defaultTake =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, null, 0, CancellationToken.None);
        defaultTake.Should().HaveCount(3);

        IReadOnlyList<AlertRecord> maxCap =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, null, 900, CancellationToken.None);
        maxCap.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListByScopeAsync_status_filter_is_case_insensitive()
    {
        InMemoryAlertRecordRepository repo = new();
        await repo.CreateAsync(
            BuildAlert(Guid.Parse("31000000-0000-0000-0000-000000000001"), AlertStatus.Open, BaseUtc, "a"),
            CancellationToken.None);
        await repo.CreateAsync(
            BuildAlert(Guid.Parse("31000000-0000-0000-0000-000000000002"), AlertStatus.Resolved, BaseUtc, "b"),
            CancellationToken.None);

        IReadOnlyList<AlertRecord> openOnly =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, "open", 50, CancellationToken.None);

        openOnly.Should().ContainSingle();
        openOnly[0].Status.Should().Be(AlertStatus.Open);
    }

    [Fact]
    public async Task ListByScopePagedAsync_returns_total_and_respects_skip_take_with_MaxPageSize_clamp()
    {
        InMemoryAlertRecordRepository repo = new();

        Task[] batch = Enumerable
            .Range(0, 10)
            .Select(n => repo.CreateAsync(
                BuildAlert(Guid.Parse($"32000000-0000-0000-0000-0000000000{n:D2}"), AlertStatus.Open,
                    BaseUtc.AddMinutes(n), $"k{n}"),
                CancellationToken.None))
            .ToArray();

        await Task.WhenAll(batch);

        (IReadOnlyList<AlertRecord> page, int total) = await repo.ListByScopePagedAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            null,
            2,
            PaginationDefaults.MaxPageSize + 50,
            CancellationToken.None);

        total.Should().Be(10);
        page.Should().HaveCount(8);
        page[0].CreatedUtc.Should().Be(BaseUtc.AddMinutes(7));

        (IReadOnlyList<AlertRecord> page2, int total2) = await repo.ListByScopePagedAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            null,
            -3,
            3,
            CancellationToken.None);

        total2.Should().Be(10);
        page2.Should().HaveCount(3);
        page2[0].CreatedUtc.Should().Be(BaseUtc.AddMinutes(9));
    }

    [Fact]
    public async Task CreateAsync_with_null_alert_throws()
    {
        InMemoryAlertRecordRepository repo = new();

        Func<Task> act = async () => await repo.CreateAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_with_null_alert_throws()
    {
        InMemoryAlertRecordRepository repo = new();

        Func<Task> act = async () => await repo.UpdateAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static AlertRecord BuildAlert(
        Guid alertId,
        string status,
        DateTime createdUtc,
        string deduplicationKey,
        Guid? tenantId = null)
    {
        return new AlertRecord
        {
            AlertId = alertId,
            TenantId = tenantId ?? TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            Title = "t",
            Category = "c",
            Severity = AlertSeverity.Warning,
            Status = status,
            TriggerValue = "v",
            Description = "d",
            CreatedUtc = createdUtc,
            DeduplicationKey = deduplicationKey
        };
    }
}
