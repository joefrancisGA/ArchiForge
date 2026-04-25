namespace ArchLucid.Decisioning.Advisory.Scheduling;

/// <summary>
///     Executes a single advisory scan for a persisted <see cref="AdvisoryScanSchedule" /> (on-demand or from a background
///     poller).
/// </summary>
/// <remarks>
///     Registered scoped in DI. Implementation lives in <c>ArchLucid.Persistence.Advisory.AdvisoryScanRunner</c>.
///     Primary callers: <c>ArchLucid.Api.Hosted.AdvisoryScanHostedService</c> (due schedules) and
///     <c>ArchLucid.Api.Controllers.AdvisorySchedulingController.RunNow</c>.
/// </remarks>
public interface IAdvisoryScanRunner
{
    /// <summary>
    ///     Creates an execution row, runs comparison/advisory/digest delivery under ambient scope, and advances
    ///     <see cref="AdvisoryScanSchedule.LastRunUtc" /> / <see cref="AdvisoryScanSchedule.NextRunUtc" /> (including after
    ///     failures).
    /// </summary>
    /// <param name="schedule">
    ///     Schedule metadata including tenant/workspace/project and
    ///     <see cref="AdvisoryScanSchedule.RunProjectSlug" />.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    ///     Pushes <c>AmbientScopeContext</c> for the schedule’s scope before authority and governance queries.
    /// </remarks>
    Task RunScheduleAsync(AdvisoryScanSchedule schedule, CancellationToken ct);
}
