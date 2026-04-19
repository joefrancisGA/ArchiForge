using ArchLucid.Core.Audit;
using ArchLucid.Core.Pagination;
using ArchLucid.Persistence.Audit;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IAuditRepository"/>.
/// </summary>
public abstract class AuditRepositoryContractTests
{
    /// <summary>Seed enough rows that repository must clamp to <see cref="PaginationDefaults.MaxListingTake"/>.</summary>
    private const int SeededAuditEventsAboveListingTakeCap = PaginationDefaults.MaxListingTake + 20;

    /// <summary>More events than <c>maxRows: 4</c> in export max-rows contract test.</summary>
    private const int SeededExportEventsForMaxRowsCapTest = 12;

    /// <summary>More events than <c>maxRows: 25</c> in export many-rows contract test.</summary>
    private const int SeededExportEventsForManyRowsTest = 80;

    protected abstract IAuditRepository CreateRepository();

    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    private static readonly Guid TenantId = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private static readonly Guid WorkspaceId = Guid.Parse("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2");
    private static readonly Guid ProjectId = Guid.Parse("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3");

    private static AuditEvent NewEvent(
        string eventType = "ContractTest",
        DateTime? occurredUtc = null,
        Guid? projectId = null,
        string? correlationId = null,
        string? actorUserId = null,
        Guid? runId = null)
    {
        return new AuditEvent
        {
            EventId = Guid.NewGuid(),
            OccurredUtc = occurredUtc ?? DateTime.UtcNow,
            EventType = eventType,
            ActorUserId = actorUserId ?? "actor",
            ActorUserName = "Actor Name",
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = projectId ?? ProjectId,
            RunId = runId,
            DataJson = "{}",
            CorrelationId = correlationId,
        };
    }

    [SkippableFact]
    public async Task Append_then_GetByScope_returns_event()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        AuditEvent evt = NewEvent();

        await repo.AppendAsync(evt, CancellationToken.None);

        IReadOnlyList<AuditEvent> list =
            await repo.GetByScopeAsync(TenantId, WorkspaceId, ProjectId, take: 50, CancellationToken.None);

        list.Should().Contain(x => x.EventId == evt.EventId);
        AuditEvent loaded = list.First(x => x.EventId == evt.EventId);
        loaded.EventType.Should().Be(evt.EventType);
        loaded.ActorUserId.Should().Be(evt.ActorUserId);
    }

    [SkippableFact]
    public async Task GetByScope_filters_other_project()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        AuditEvent matching = NewEvent();
        AuditEvent other = NewEvent(projectId: Guid.NewGuid());

        await repo.AppendAsync(matching, CancellationToken.None);
        await repo.AppendAsync(other, CancellationToken.None);

        IReadOnlyList<AuditEvent> list =
            await repo.GetByScopeAsync(TenantId, WorkspaceId, ProjectId, take: 50, CancellationToken.None);

        list.Should().Contain(x => x.EventId == matching.EventId);
        list.Should().NotContain(x => x.EventId == other.EventId);
    }

    [SkippableFact]
    public async Task GetByScope_orders_by_OccurredUtc_descending()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        Guid isolatedProjectId = Guid.NewGuid();
        DateTime older = DateTime.UtcNow.AddMinutes(-10);
        DateTime newer = DateTime.UtcNow.AddMinutes(-5);
        AuditEvent first = NewEvent(occurredUtc: older, projectId: isolatedProjectId);
        AuditEvent second = NewEvent(occurredUtc: newer, projectId: isolatedProjectId);

        await repo.AppendAsync(first, CancellationToken.None);
        await repo.AppendAsync(second, CancellationToken.None);

        IReadOnlyList<AuditEvent> list =
            await repo.GetByScopeAsync(TenantId, WorkspaceId, isolatedProjectId, take: 10, CancellationToken.None);

        int iOld = list.ToList().FindIndex(x => x.EventId == first.EventId);
        int iNew = list.ToList().FindIndex(x => x.EventId == second.EventId);
        iOld.Should().BeGreaterThan(iNew);
    }

    [SkippableFact]
    public async Task GetFilteredAsync_ByEventType_ReturnsOnlyMatchingType()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        AuditEvent match = NewEvent(eventType: "TypeA");
        AuditEvent other = NewEvent(eventType: "TypeB");

        await repo.AppendAsync(match, CancellationToken.None);
        await repo.AppendAsync(other, CancellationToken.None);

        AuditEventFilter filter = new() { EventType = "TypeA", Take = 50 };
        IReadOnlyList<AuditEvent> list =
            await repo.GetFilteredAsync(TenantId, WorkspaceId, ProjectId, filter, CancellationToken.None);

        list.Should().Contain(x => x.EventId == match.EventId);
        list.Should().NotContain(x => x.EventId == other.EventId);
    }

    [SkippableFact]
    public async Task GetFilteredAsync_ByDateRange_FiltersCorrectly()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        DateTime mid = DateTime.UtcNow.AddMinutes(-30);
        AuditEvent before = NewEvent(occurredUtc: mid.AddHours(-2));
        AuditEvent inside = NewEvent(occurredUtc: mid);
        AuditEvent after = NewEvent(occurredUtc: mid.AddHours(2));

        await repo.AppendAsync(before, CancellationToken.None);
        await repo.AppendAsync(inside, CancellationToken.None);
        await repo.AppendAsync(after, CancellationToken.None);

        AuditEventFilter filter = new()
        {
            FromUtc = mid.AddHours(-1),
            ToUtc = mid.AddHours(1),
            Take = 50,
        };

        IReadOnlyList<AuditEvent> list =
            await repo.GetFilteredAsync(TenantId, WorkspaceId, ProjectId, filter, CancellationToken.None);

        list.Should().Contain(x => x.EventId == inside.EventId);
        list.Should().NotContain(x => x.EventId == before.EventId);
        list.Should().NotContain(x => x.EventId == after.EventId);
    }

    [SkippableFact]
    public async Task GetFilteredAsync_ByCorrelationId_ReturnsMatch()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        AuditEvent match = NewEvent(correlationId: "corr-xyz");
        AuditEvent other = NewEvent(correlationId: "other");

        await repo.AppendAsync(match, CancellationToken.None);
        await repo.AppendAsync(other, CancellationToken.None);

        AuditEventFilter filter = new() { CorrelationId = "corr-xyz", Take = 50 };
        IReadOnlyList<AuditEvent> list =
            await repo.GetFilteredAsync(TenantId, WorkspaceId, ProjectId, filter, CancellationToken.None);

        list.Should().Contain(x => x.EventId == match.EventId);
        list.Should().NotContain(x => x.EventId == other.EventId);
    }

    [SkippableFact]
    public async Task GetFilteredAsync_ByActorUserId_ReturnsMatch()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        AuditEvent match = NewEvent(actorUserId: "user-99");
        AuditEvent other = NewEvent(actorUserId: "user-1");

        await repo.AppendAsync(match, CancellationToken.None);
        await repo.AppendAsync(other, CancellationToken.None);

        AuditEventFilter filter = new() { ActorUserId = "user-99", Take = 50 };
        IReadOnlyList<AuditEvent> list =
            await repo.GetFilteredAsync(TenantId, WorkspaceId, ProjectId, filter, CancellationToken.None);

        list.Should().Contain(x => x.EventId == match.EventId);
        list.Should().NotContain(x => x.EventId == other.EventId);
    }

    [SkippableFact]
    public async Task GetFilteredAsync_ByRunId_ReturnsMatch()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        Guid run = Guid.NewGuid();
        AuditEvent match = NewEvent(runId: run);
        AuditEvent other = NewEvent(runId: Guid.NewGuid());

        await repo.AppendAsync(match, CancellationToken.None);
        await repo.AppendAsync(other, CancellationToken.None);

        AuditEventFilter filter = new() { RunId = run, Take = 50 };
        IReadOnlyList<AuditEvent> list =
            await repo.GetFilteredAsync(TenantId, WorkspaceId, ProjectId, filter, CancellationToken.None);

        list.Should().Contain(x => x.EventId == match.EventId);
        list.Should().NotContain(x => x.EventId == other.EventId);
    }

    [SkippableFact]
    public async Task GetFilteredAsync_MultipleFilters_AppliesAll()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        Guid run = Guid.NewGuid();
        DateTime t = DateTime.UtcNow.AddMinutes(-5);
        AuditEvent match = NewEvent(
            eventType: "MultiFilter",
            occurredUtc: t,
            correlationId: "c1",
            actorUserId: "a1",
            runId: run);
        AuditEvent wrongType = NewEvent(
            eventType: "Other",
            occurredUtc: t,
            correlationId: "c1",
            actorUserId: "a1",
            runId: run);

        await repo.AppendAsync(match, CancellationToken.None);
        await repo.AppendAsync(wrongType, CancellationToken.None);

        AuditEventFilter filter = new()
        {
            EventType = "MultiFilter",
            FromUtc = t.AddMinutes(-10),
            ToUtc = t.AddMinutes(10),
            CorrelationId = "c1",
            ActorUserId = "a1",
            RunId = run,
            Take = 50,
        };

        IReadOnlyList<AuditEvent> list =
            await repo.GetFilteredAsync(TenantId, WorkspaceId, ProjectId, filter, CancellationToken.None);

        list.Should().ContainSingle(x => x.EventId == match.EventId);
    }

    [SkippableFact]
    public async Task GetFilteredAsync_NoFilters_ReturnsAllInScopeOrderedDesc()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        Guid isolatedProjectId = Guid.NewGuid();
        DateTime older = DateTime.UtcNow.AddMinutes(-20);
        DateTime newer = DateTime.UtcNow.AddMinutes(-10);
        AuditEvent first = NewEvent(occurredUtc: older, projectId: isolatedProjectId);
        AuditEvent second = NewEvent(occurredUtc: newer, projectId: isolatedProjectId);

        await repo.AppendAsync(first, CancellationToken.None);
        await repo.AppendAsync(second, CancellationToken.None);

        AuditEventFilter filter = new() { Take = 10 };
        IReadOnlyList<AuditEvent> list =
            await repo.GetFilteredAsync(TenantId, WorkspaceId, isolatedProjectId, filter, CancellationToken.None);

        list.Should().Contain(x => x.EventId == first.EventId);
        list.Should().Contain(x => x.EventId == second.EventId);
        int iOld = list.ToList().FindIndex(x => x.EventId == first.EventId);
        int iNew = list.ToList().FindIndex(x => x.EventId == second.EventId);
        iOld.Should().BeGreaterThan(iNew);
    }

    [SkippableFact]
    public async Task GetFilteredAsync_null_filter_throws()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();

        Func<Task> act = () => repo.GetFilteredAsync(
            TenantId,
            WorkspaceId,
            ProjectId,
            null!,
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [SkippableFact]
    public async Task GetFilteredAsync_clamps_take_to_500()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        for (int i = 0; i < SeededAuditEventsAboveListingTakeCap; i++)
        {
            await repo.AppendAsync(NewEvent(eventType: "Bulk"), CancellationToken.None);
        }

        AuditEventFilter filter = new() { EventType = "Bulk", Take = 10_000 };
        IReadOnlyList<AuditEvent> list =
            await repo.GetFilteredAsync(TenantId, WorkspaceId, ProjectId, filter, CancellationToken.None);

        list.Count.Should().Be(PaginationDefaults.MaxListingTake);
    }

    [SkippableFact]
    public async Task GetExportAsync_DateRange_IsHalfOpen_OnOccurredUtc()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        DateTime mid = DateTime.UtcNow.AddMinutes(-40);
        AuditEvent before = NewEvent(occurredUtc: mid.AddHours(-2));
        AuditEvent inside = NewEvent(occurredUtc: mid);
        AuditEvent after = NewEvent(occurredUtc: mid.AddHours(2));

        await repo.AppendAsync(before, CancellationToken.None);
        await repo.AppendAsync(inside, CancellationToken.None);
        await repo.AppendAsync(after, CancellationToken.None);

        DateTime from = mid.AddHours(-1);
        DateTime to = mid.AddHours(1);
        IReadOnlyList<AuditEvent> list =
            await repo.GetExportAsync(TenantId, WorkspaceId, ProjectId, from, to, maxRows: 100, CancellationToken.None);

        list.Should().Contain(x => x.EventId == inside.EventId);
        list.Should().NotContain(x => x.EventId == before.EventId);
        list.Should().NotContain(x => x.EventId == after.EventId);
    }

    [SkippableFact]
    public async Task GetExportAsync_OrdersByOccurredUtc_Ascending()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        Guid isolatedProjectId = Guid.NewGuid();
        DateTime older = DateTime.UtcNow.AddMinutes(-25);
        DateTime newer = DateTime.UtcNow.AddMinutes(-15);
        AuditEvent first = NewEvent(occurredUtc: older, projectId: isolatedProjectId);
        AuditEvent second = NewEvent(occurredUtc: newer, projectId: isolatedProjectId);

        await repo.AppendAsync(second, CancellationToken.None);
        await repo.AppendAsync(first, CancellationToken.None);

        DateTime from = older.AddMinutes(-10);
        DateTime to = newer.AddMinutes(10);
        IReadOnlyList<AuditEvent> list =
            await repo.GetExportAsync(
                TenantId,
                WorkspaceId,
                isolatedProjectId,
                from,
                to,
                maxRows: 50,
                CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].EventId.Should().Be(first.EventId);
        list[1].EventId.Should().Be(second.EventId);
    }

    [SkippableFact]
    public async Task GetExportAsync_RespectsMaxRows()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        Guid isolatedProjectId = Guid.NewGuid();
        DateTime t0 = DateTime.UtcNow.AddHours(-3);

        for (int i = 0; i < SeededExportEventsForMaxRowsCapTest; i++)
        {
            await repo.AppendAsync(
                NewEvent(eventType: "ExportCap", occurredUtc: t0.AddMinutes(i), projectId: isolatedProjectId),
                CancellationToken.None);
        }

        DateTime from = t0.AddMinutes(-1);
        DateTime to = t0.AddMinutes(20);
        IReadOnlyList<AuditEvent> list =
            await repo.GetExportAsync(
                TenantId,
                WorkspaceId,
                isolatedProjectId,
                from,
                to,
                maxRows: 4,
                CancellationToken.None);

        list.Should().HaveCount(4);
    }

    [SkippableFact]
    public async Task GetExportAsync_WhenMoreRowsExistThanMax_ReturnsMaxRows()
    {
        SkipIfSqlServerUnavailable();
        IAuditRepository repo = CreateRepository();
        Guid isolatedProjectId = Guid.NewGuid();
        DateTime t0 = DateTime.UtcNow.AddHours(-8);

        for (int i = 0; i < SeededExportEventsForManyRowsTest; i++)
        {
            await repo.AppendAsync(
                NewEvent(eventType: "ExportMany", occurredUtc: t0.AddSeconds(i), projectId: isolatedProjectId),
                CancellationToken.None);
        }

        DateTime from = t0.AddSeconds(-1);
        DateTime to = t0.AddSeconds(200);
        IReadOnlyList<AuditEvent> list =
            await repo.GetExportAsync(
                TenantId,
                WorkspaceId,
                isolatedProjectId,
                from,
                to,
                maxRows: 25,
                CancellationToken.None);

        list.Should().HaveCount(25);
    }
}
