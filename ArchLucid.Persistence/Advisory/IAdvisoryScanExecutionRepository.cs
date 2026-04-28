using ArchLucid.Decisioning.Advisory.Scheduling;

namespace ArchLucid.Persistence;

/// <summary>
/// Persistence for <see cref="AdvisoryScanExecution"/> audit/history rows created by <see cref="IAdvisoryScanRunner"/>.
/// </summary>
/// <remarks>
/// SQL: <c>DapperAdvisoryScanExecutionRepository</c>; in-memory: <c>InMemoryAdvisoryScanExecutionRepository</c>.
/// Primary callers: <c>ArchLucid.Application.Advisory.AdvisoryScanRunner</c>,
/// <c>ArchLucid.Api.Controllers.AdvisorySchedulingController</c> (list by schedule).
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
