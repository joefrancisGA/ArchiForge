using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Advisory;

namespace ArchiForge.Host.Core.Hosted;

/// <summary>
/// One poll iteration: load due advisory schedules and invoke <see cref="IAdvisoryScanRunner"/> for each (sequential, no in-iteration parallelism).
/// </summary>
/// <remarks>
/// Extracted from <see cref="AdvisoryScanHostedService"/> for unit tests (failure isolation, cancellation, ordering) without spinning the full background loop.
/// </remarks>
public sealed class AdvisoryDueScheduleProcessor(
    IAdvisoryScanScheduleRepository scheduleRepository,
    IAdvisoryScanRunner runner,
    ILogger<AdvisoryDueScheduleProcessor> logger)
{
    /// <summary>
    /// Loads up to <paramref name="maxSchedules"/> due rows and runs each; per-schedule errors are logged and swallowed except <see cref="OperationCanceledException"/>.
    /// </summary>
    public async Task ProcessDueAsync(DateTime utcNow, int maxSchedules, CancellationToken ct)
    {
        IReadOnlyList<AdvisoryScanSchedule> due = await scheduleRepository
            .ListDueAsync(utcNow, maxSchedules, ct)
            ;

        foreach (AdvisoryScanSchedule schedule in due)
        
            try
            {
                await runner.RunScheduleAsync(schedule, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Advisory scan failed for schedule {ScheduleId}.", schedule.ScheduleId);
            }
        
    }
}
