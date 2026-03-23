using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Persistence.Advisory;

/// <summary>
/// Persistence for <see cref="AdvisoryScanExecution"/> audit/history rows created by <see cref="IAdvisoryScanRunner"/>.
/// </summary>
/// <remarks>
/// SQL: <see cref="DapperAdvisoryScanExecutionRepository"/>; in-memory: <see cref="InMemoryAdvisoryScanExecutionRepository"/>.
/// Primary callers: <c>ArchiForge.Persistence.Advisory.AdvisoryScanRunner</c>, <c>ArchiForge.Api.Controllers.AdvisorySchedulingController</c> (list by schedule).
/// </remarks>
public interface IAdvisoryScanExecutionRepository
{
    /// <summary>Inserts a new execution row (typically status <c>Started</c>).</summary>
    Task CreateAsync(AdvisoryScanExecution execution, CancellationToken ct);

    /// <summary>Updates completion time, status, result JSON, and optional error message.</summary>
    Task UpdateAsync(AdvisoryScanExecution execution, CancellationToken ct);

    /// <summary>
    /// Returns up to <paramref name="take"/> executions for the schedule, newest <see cref="AdvisoryScanExecution.StartedUtc"/> first.
    /// </summary>
    Task<IReadOnlyList<AdvisoryScanExecution>> ListByScheduleAsync(
        Guid scheduleId,
        int take,
        CancellationToken ct);
}
