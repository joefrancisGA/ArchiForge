using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Persistence.Advisory;

/// <summary>
/// Thread-safe in-memory <see cref="IAdvisoryScanScheduleRepository"/> for tests and storage-off mode.
/// </summary>
public sealed class InMemoryAdvisoryScanScheduleRepository : IAdvisoryScanScheduleRepository
{
    private readonly List<AdvisoryScanSchedule> _items = [];
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(AdvisoryScanSchedule schedule, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _items.Add(schedule);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AdvisoryScanSchedule schedule, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var i = _items.FindIndex(x => x.ScheduleId == schedule.ScheduleId);
            if (i >= 0)
                _items[i] = schedule;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AdvisoryScanSchedule>> ListDueAsync(
        DateTime utcNow,
        int take,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(s =>
                    s is { IsEnabled: true, NextRunUtc: not null } &&
                    s.NextRunUtc <= utcNow)
                .OrderBy(s => s.NextRunUtc)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<AdvisoryScanSchedule>>(result);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AdvisoryScanSchedule>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(s =>
                    s.TenantId == tenantId &&
                    s.WorkspaceId == workspaceId &&
                    s.ProjectId == projectId)
                .OrderByDescending(s => s.CreatedUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<AdvisoryScanSchedule>>(result);
        }
    }

    /// <inheritdoc />
    public Task<AdvisoryScanSchedule?> GetByIdAsync(Guid scheduleId, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            return Task.FromResult(_items.FirstOrDefault(x => x.ScheduleId == scheduleId));
    }
}
