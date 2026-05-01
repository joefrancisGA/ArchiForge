using System.Globalization;

namespace ArchLucid.Application.Pilots;

/// <summary>Resolves inclusive UTC quarter windows for <see cref="BoardPackPdfBuilder" />.</summary>
public static class BoardPackQuarterWindow
{
    /// <summary>
    ///     Returns UTC half-open window <c>[start, end)</c> for the calendar quarter, or overridden bounds when both are
    ///     set.
    /// </summary>
    public static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) Resolve(
        int year,
        int quarter,
        DateTimeOffset? overrideStartUtc,
        DateTimeOffset? overrideEndUtc)
    {
        if (overrideStartUtc is { } os && overrideEndUtc is { } oe)
        {
            return oe <= os
                ? throw new ArgumentOutOfRangeException(nameof(overrideEndUtc),
                    "Override window end must be after start.")
                : (os, oe);
        }

        if (year is < 2000 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(year));

        if (quarter is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(quarter));

        int startMonth = (quarter - 1) * 3 + 1;
        DateTimeOffset start = new(year, startMonth, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset end = start.AddMonths(3);

        return (start, end);
    }

    /// <summary>
    ///     Pick one ISO week inside the quarter (mid-quarter anchor) for <see cref="ExecDigest.ExecDigestComposer" />
    ///     reuse.
    /// </summary>
    public static (DateTime WeekStartUtcInclusive, DateTime WeekEndUtcExclusive) DigestWeekInsideQuarter(
        DateTimeOffset quarterStartUtc,
        DateTimeOffset quarterEndUtc)
    {
        TimeSpan span = quarterEndUtc - quarterStartUtc;
        DateTimeOffset mid = quarterStartUtc + TimeSpan.FromTicks(span.Ticks / 2);
        DateTime refDay = DateTime.SpecifyKind(mid.UtcDateTime.Date, DateTimeKind.Utc);
        int isoYear = ISOWeek.GetYear(refDay);
        int isoWeek = ISOWeek.GetWeekOfYear(refDay);
        DateTime weekStartUtc =
            DateTime.SpecifyKind(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday), DateTimeKind.Utc);
        DateTime weekEndUtc = weekStartUtc.AddDays(7);

        return (weekStartUtc, weekEndUtc);
    }
}
