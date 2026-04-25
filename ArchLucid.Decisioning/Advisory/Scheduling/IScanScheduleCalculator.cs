namespace ArchLucid.Decisioning.Advisory.Scheduling;

/// <summary>
///     Computes the next UTC run time from a v1 “cron” string after a reference instant (typically “now” after a scan).
/// </summary>
/// <remarks>
///     Registered scoped in DI. Default implementation: <see cref="SimpleScanScheduleCalculator" />.
///     Used when creating schedules and when <c>AdvisoryScanRunner</c> advances
///     <see cref="AdvisoryScanSchedule.NextRunUtc" />.
/// </remarks>
public interface IScanScheduleCalculator
{
    /// <summary>
    ///     Returns the next eligible run instant in UTC after <paramref name="fromUtc" /> (nullable for implementations that
    ///     may reject invalid expressions).
    /// </summary>
    /// <param name="cronExpression">Expression or alias (e.g. <c>@daily</c>, <c>0 7 * * *</c>).</param>
    /// <param name="fromUtc">
    ///     Reference instant; <see cref="SimpleScanScheduleCalculator" /> treats <c>0 7 * * *</c> as the
    ///     next 07:00 UTC boundary on or after this time.
    /// </param>
    /// <returns>Next run UTC, or <see langword="null" /> when the expression is not schedulable.</returns>
    DateTime? ComputeNextRunUtc(string cronExpression, DateTime fromUtc);
}
