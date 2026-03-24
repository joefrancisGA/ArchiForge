using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Persistence.Advisory;

/// <summary>
/// Thread-safe in-memory <see cref="IAdvisoryScanScheduleRepository"/> for tests and storage-off mode.
/// </summary>
public sealed class InMemoryAdvisoryScanScheduleRepository : IAdvisoryScanScheduleRepository
{
    private const int MaxEntries = 2_000;

    private readonly List<AdvisoryScanSchedule> _items = [];
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(AdvisoryScanSchedule schedule, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_items.Count >= MaxEntries)
                _items.RemoveAt(0);

            _items.Add(schedule);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(AdvisoryScanSchedule schedule, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ct.ThrowIfCancellationRequested();
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
        ct.ThrowIfCancellationRequested();
        var n = Math.Clamp(take <= 0 ? 20 : take, 1, 200);
        lock (_gate)
        {
            var result = _items
                .Where(s =>
                    s is { IsEnabled: true, NextRunUtc: not null } &&
                    s.NextRunUtc <= utcNow)
                .OrderBy(s => s.NextRunUtc)
                .Take(n)
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
        ct.ThrowIfCancellationRequested();
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
        ct.ThrowIfCancellationRequested();
        lock (_gate)
            return Task.FromResult(_items.FirstOrDefault(x => x.ScheduleId == scheduleId));
    }
}
