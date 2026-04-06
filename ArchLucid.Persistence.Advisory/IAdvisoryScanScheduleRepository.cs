using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Persistence.Advisory;

/// <summary>
/// Persistence for <see cref="AdvisoryScanSchedule"/> rows (CRUD subset used by API and the advisory scan runner).
/// </summary>
/// <remarks>
/// SQL: <see cref="DapperAdvisoryScanScheduleRepository"/>; in-memory: <see cref="InMemoryAdvisoryScanScheduleRepository"/>.
/// Primary callers: <c>ArchiForge.Api.Controllers.AdvisorySchedulingController</c>, <c>ArchiForge.Api.Hosted.AdvisoryScanHostedService</c>, <c>ArchiForge.Persistence.Advisory.AdvisoryScanRunner</c>.
/// </remarks>
public interface IAdvisoryScanScheduleRepository
{
    /// <summary>Inserts a new schedule row.</summary>
    Task CreateAsync(AdvisoryScanSchedule schedule, CancellationToken ct);

    /// <summary>Updates mutable fields (name, cron, enabled, slug, last/next run).</summary>
    Task UpdateAsync(AdvisoryScanSchedule schedule, CancellationToken ct);

    /// <summary>
    /// Returns up to <paramref name="take"/> enabled schedules with <see cref="AdvisoryScanSchedule.NextRunUtc"/> ≤ <paramref name="utcNow"/>, oldest due first.
    /// </summary>
    /// <param name="utcNow">“As of” instant for due comparison (use UTC).</param>
    /// <param name="take">Maximum rows (poller uses a small batch).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Due schedules; may be empty.</returns>
    Task<IReadOnlyList<AdvisoryScanSchedule>> ListDueAsync(
        DateTime utcNow,
        int take,
        CancellationToken ct);

    /// <summary>Lists schedules for a tenant/workspace/project, newest <see cref="AdvisoryScanSchedule.CreatedUtc"/> first.</summary>
    Task<IReadOnlyList<AdvisoryScanSchedule>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    /// <summary>Loads a schedule by id, or <see langword="null"/> if missing.</summary>
    Task<AdvisoryScanSchedule?> GetByIdAsync(Guid scheduleId, CancellationToken ct);
}
