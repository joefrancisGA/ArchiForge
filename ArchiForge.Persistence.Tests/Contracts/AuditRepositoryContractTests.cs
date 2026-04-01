using ArchiForge.Core.Audit;
using ArchiForge.Persistence.Audit;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IAuditRepository"/>.
/// </summary>
public abstract class AuditRepositoryContractTests
{
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
        Guid? projectId = null)
    {
        return new AuditEvent
        {
            EventId = Guid.NewGuid(),
            OccurredUtc = occurredUtc ?? DateTime.UtcNow,
            EventType = eventType,
            ActorUserId = "actor",
            ActorUserName = "Actor Name",
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = projectId ?? ProjectId,
            DataJson = "{}"
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
        DateTime older = DateTime.UtcNow.AddMinutes(-10);
        DateTime newer = DateTime.UtcNow.AddMinutes(-5);
        AuditEvent first = NewEvent(occurredUtc: older);
        AuditEvent second = NewEvent(occurredUtc: newer);

        await repo.AppendAsync(first, CancellationToken.None);
        await repo.AppendAsync(second, CancellationToken.None);

        IReadOnlyList<AuditEvent> list =
            await repo.GetByScopeAsync(TenantId, WorkspaceId, ProjectId, take: 10, CancellationToken.None);

        int iOld = list.ToList().FindIndex(x => x.EventId == first.EventId);
        int iNew = list.ToList().FindIndex(x => x.EventId == second.EventId);
        iOld.Should().BeGreaterThan(iNew);
    }
}
