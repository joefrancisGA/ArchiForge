using ArchiForge.Core.Resilience;

using FluentAssertions;

namespace ArchiForge.Retrieval.Tests;

/// <summary>
/// Deterministic tests for <see cref="CircuitBreakerGate"/> using an injectable clock.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CircuitBreakerGateTests
{
    [Fact]
    public void Closed_after_threshold_failures_opens_on_next_failure()
    {
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions options = new() { FailureThreshold = 3, DurationOfBreakSeconds = 10 };
        CircuitBreakerGate gate = new(options, clock.ToFunc());

        gate.ThrowIfBroken();
        gate.RecordFailure();
        gate.ThrowIfBroken();
        gate.RecordFailure();
        gate.ThrowIfBroken();
        gate.RecordFailure();

        Action act = () => gate.ThrowIfBroken();

        act.Should().Throw<CircuitBreakerOpenException>();
    }

    [Fact]
    public void Open_after_duration_allows_single_probe_then_closes_on_success()
    {
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 30 };
        CircuitBreakerGate gate = new(options, clock.ToFunc());

        gate.ThrowIfBroken();
        gate.RecordFailure();
        Action whileOpen = () => gate.ThrowIfBroken();
        whileOpen.Should().Throw<CircuitBreakerOpenException>();

        clock.Advance(TimeSpan.FromSeconds(31));
        gate.ThrowIfBroken();
        gate.RecordSuccess();
        gate.ThrowIfBroken();
    }

    [Fact]
    public void Half_open_failure_reopens_circuit()
    {
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 30 };
        CircuitBreakerGate gate = new(options, clock.ToFunc());

        gate.RecordFailure();
        clock.Advance(TimeSpan.FromSeconds(31));
        gate.ThrowIfBroken();
        gate.RecordFailure();

        Action act = () => gate.ThrowIfBroken();

        act.Should().Throw<CircuitBreakerOpenException>();
    }

    [Fact]
    public async Task Concurrent_second_ThrowIfBroken_while_probe_in_flight_rejected()
    {
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate gate = new(options, clock.ToFunc());

        gate.RecordFailure();
        clock.Advance(TimeSpan.FromMinutes(2));
        gate.ThrowIfBroken();

        Task<bool> secondCaller = Task.Run(() =>
        {
            try
            {
                gate.ThrowIfBroken();
                return false;
            }
            catch (CircuitBreakerOpenException)
            {
                return true;
            }
        });

        bool rejected = await secondCaller;

        rejected.Should().BeTrue();
        gate.RecordSuccess();
    }

    [Fact]
    public void RecordCallCancelled_releases_probe_and_allows_immediate_retry_window()
    {
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate gate = new(options, clock.ToFunc());

        gate.RecordFailure();
        clock.Advance(TimeSpan.FromMinutes(2));
        gate.ThrowIfBroken();
        gate.RecordCallCancelled();

        gate.ThrowIfBroken();
        gate.RecordSuccess();
    }
}
