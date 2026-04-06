namespace ArchiForge.Decisioning.Advisory.Scheduling;

/// <summary>
/// Minimal schedule interpreter: a few named aliases, fixed <c>0 7 * * *</c> (07:00 UTC daily), and a default of +1 day for any other token.
/// </summary>
/// <remarks>
/// Not a full CRON engine; unknown patterns fall back to <see cref="DateTime"/> day increments. Prefer documented expressions for predictable scans.
/// </remarks>
public sealed class SimpleScanScheduleCalculator : IScanScheduleCalculator
{
    /// <inheritdoc />
    public DateTime? ComputeNextRunUtc(string cronExpression, DateTime fromUtc)
    {
        string cron = cronExpression.Trim();
        return cron switch
        {
            "@hourly" => fromUtc.AddHours(1),
            "@daily" => fromUtc.AddDays(1),
            "@weekly" => fromUtc.AddDays(7),
            "0 7 * * *" => NextDailyAtSevenAmUtc(fromUtc),
            _ => fromUtc.AddDays(1)
        };
    }

    private static DateTime NextDailyAtSevenAmUtc(DateTime fromUtc)
    {
        DateTime todaySeven = fromUtc.Date.AddHours(7);
        return fromUtc < todaySeven ? todaySeven : fromUtc.Date.AddDays(1).AddHours(7);
    }
}
