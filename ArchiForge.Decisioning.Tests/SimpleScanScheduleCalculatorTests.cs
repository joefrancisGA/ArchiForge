using ArchiForge.Decisioning.Advisory.Scheduling;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class SimpleScanScheduleCalculatorTests
{
    private readonly SimpleScanScheduleCalculator _sut = new();

    [Fact]
    public void ComputeNextRunUtc_Hourly_AddsOneHour()
    {
        DateTime from = new(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc);

        DateTime? next = _sut.ComputeNextRunUtc("@hourly", from);

        next.Should().Be(from.AddHours(1));
    }

    [Fact]
    public void ComputeNextRunUtc_Daily_AddsOneDay()
    {
        DateTime from = new(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc);

        DateTime? next = _sut.ComputeNextRunUtc("@daily", from);

        next.Should().Be(from.AddDays(1));
    }

    [Fact]
    public void ComputeNextRunUtc_Weekly_AddsSevenDays()
    {
        DateTime from = new(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc);

        DateTime? next = _sut.ComputeNextRunUtc("@weekly", from);

        next.Should().Be(from.AddDays(7));
    }

    [Fact]
    public void ComputeNextRunUtc_DailyAtSeven_BeforeSevenSameDay_ReturnsSevenToday()
    {
        DateTime from = new(2026, 3, 26, 5, 0, 0, DateTimeKind.Utc);

        DateTime? next = _sut.ComputeNextRunUtc("0 7 * * *", from);

        next.Should().Be(new DateTime(2026, 3, 26, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ComputeNextRunUtc_DailyAtSeven_AfterSeven_ReturnsSevenNextDay()
    {
        DateTime from = new(2026, 3, 26, 8, 0, 0, DateTimeKind.Utc);

        DateTime? next = _sut.ComputeNextRunUtc("0 7 * * *", from);

        next.Should().Be(new DateTime(2026, 3, 27, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ComputeNextRunUtc_UnknownExpression_AddsOneDay()
    {
        DateTime from = new(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc);

        DateTime? next = _sut.ComputeNextRunUtc("not-a-real-cron", from);

        next.Should().Be(from.AddDays(1));
    }

    [Fact]
    public void ComputeNextRunUtc_TrimsWhitespace()
    {
        DateTime from = new(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc);

        DateTime? next = _sut.ComputeNextRunUtc("  @daily  ", from);

        next.Should().Be(from.AddDays(1));
    }
}
