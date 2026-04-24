using ArchLucid.Decisioning.Advisory.Scheduling;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IAdvisoryScanScheduleRepository" />.
/// </summary>
public abstract class AdvisoryScanScheduleRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IAdvisoryScanScheduleRepository CreateRepository();

    [SkippableFact]
    public async Task Create_then_GetById_round_trips()
    {
        SkipIfSqlServerUnavailable();
        IAdvisoryScanScheduleRepository repo = CreateRepository();
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        AdvisoryScanSchedule schedule = NewSchedule(tenantId, workspaceId, projectId);

        await repo.CreateAsync(schedule, CancellationToken.None);

        AdvisoryScanSchedule? loaded = await repo.GetByIdAsync(schedule.ScheduleId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.ScheduleId.Should().Be(schedule.ScheduleId);
        loaded.Name.Should().Be(schedule.Name);
        loaded.CronExpression.Should().Be(schedule.CronExpression);
    }

    [SkippableFact]
    public async Task Update_is_visible_in_GetById()
    {
        SkipIfSqlServerUnavailable();
        IAdvisoryScanScheduleRepository repo = CreateRepository();
        Guid tenantId = Guid.NewGuid();
        AdvisoryScanSchedule schedule = NewSchedule(tenantId, Guid.NewGuid(), Guid.NewGuid());

        await repo.CreateAsync(schedule, CancellationToken.None);

        schedule.Name = "Updated name";
        schedule.NextRunUtc = DateTime.UtcNow.AddHours(6);
        await repo.UpdateAsync(schedule, CancellationToken.None);

        AdvisoryScanSchedule? loaded = await repo.GetByIdAsync(schedule.ScheduleId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Name.Should().Be("Updated name");
        loaded.NextRunUtc.Should().BeCloseTo(schedule.NextRunUtc!.Value, TimeSpan.FromSeconds(2));
    }

    [SkippableFact]
    public async Task ListByScope_returns_only_matching_tenant_workspace_project()
    {
        SkipIfSqlServerUnavailable();
        IAdvisoryScanScheduleRepository repo = CreateRepository();
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        AdvisoryScanSchedule a = NewSchedule(tenantId, workspaceId, projectId);
        AdvisoryScanSchedule b = NewSchedule(tenantId, workspaceId, Guid.NewGuid());

        await repo.CreateAsync(a, CancellationToken.None);
        await repo.CreateAsync(b, CancellationToken.None);

        IReadOnlyList<AdvisoryScanSchedule> list = await repo.ListByScopeAsync(
            tenantId,
            workspaceId,
            projectId,
            CancellationToken.None);

        list.Should().ContainSingle(s => s.ScheduleId == a.ScheduleId);
    }

    [SkippableFact]
    public async Task ListDue_returns_enabled_schedules_with_NextRunUtc_not_after_utcNow_ordered_oldest_first()
    {
        SkipIfSqlServerUnavailable();
        IAdvisoryScanScheduleRepository repo = CreateRepository();
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        DateTime utcNow = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

        AdvisoryScanSchedule dueFirst = NewSchedule(tenantId, workspaceId, projectId);
        dueFirst.NextRunUtc = utcNow.AddHours(-2);
        dueFirst.IsEnabled = true;

        AdvisoryScanSchedule dueSecond = NewSchedule(tenantId, workspaceId, projectId);
        dueSecond.NextRunUtc = utcNow.AddHours(-1);
        dueSecond.IsEnabled = true;

        AdvisoryScanSchedule future = NewSchedule(tenantId, workspaceId, projectId);
        future.NextRunUtc = utcNow.AddHours(1);
        future.IsEnabled = true;

        AdvisoryScanSchedule disabledDue = NewSchedule(tenantId, workspaceId, projectId);
        disabledDue.NextRunUtc = utcNow.AddHours(-3);
        disabledDue.IsEnabled = false;

        await repo.CreateAsync(dueFirst, CancellationToken.None);
        await repo.CreateAsync(dueSecond, CancellationToken.None);
        await repo.CreateAsync(future, CancellationToken.None);
        await repo.CreateAsync(disabledDue, CancellationToken.None);

        IReadOnlyList<AdvisoryScanSchedule> due = await repo.ListDueAsync(utcNow, 10, CancellationToken.None);

        List<Guid> ids = due.Select(s => s.ScheduleId).ToList();
        ids.Should().Contain(dueFirst.ScheduleId);
        ids.Should().Contain(dueSecond.ScheduleId);
        ids.Should().NotContain(future.ScheduleId);
        ids.Should().NotContain(disabledDue.ScheduleId);

        int i1 = ids.IndexOf(dueFirst.ScheduleId);
        int i2 = ids.IndexOf(dueSecond.ScheduleId);
        i1.Should().BeLessThan(i2);
    }

    private static AdvisoryScanSchedule NewSchedule(Guid tenantId, Guid workspaceId, Guid projectId)
    {
        return new AdvisoryScanSchedule
        {
            ScheduleId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            Name = "Contract schedule",
            CronExpression = "0 7 * * *",
            IsEnabled = true,
            NextRunUtc = DateTime.UtcNow.AddDays(1),
            CreatedUtc = DateTime.UtcNow
        };
    }
}
