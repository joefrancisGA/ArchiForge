using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Persistence.Advisory;

public sealed class InMemoryAdvisoryScanExecutionRepository : IAdvisoryScanExecutionRepository
{
    private readonly List<AdvisoryScanExecution> _items = [];
    private readonly Lock _gate = new();

    public Task CreateAsync(AdvisoryScanExecution execution, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
            _items.Add(execution);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AdvisoryScanExecution execution, CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var i = _items.FindIndex(x => x.ExecutionId == execution.ExecutionId);
            if (i >= 0)
                _items[i] = execution;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AdvisoryScanExecution>> ListByScheduleAsync(
        Guid scheduleId,
        int take,
        CancellationToken ct)
    {
        _ = ct;
        lock (_gate)
        {
            var result = _items
                .Where(x => x.ScheduleId == scheduleId)
                .OrderByDescending(x => x.StartedUtc)
                .Take(take)
                .ToList();

            return Task.FromResult<IReadOnlyList<AdvisoryScanExecution>>(result);
        }
    }
}
