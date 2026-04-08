using ArchLucid.Core.Resilience;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Resilience;

[Trait("Suite", "Core")]
public sealed class CircuitBreakerGateAuditCallbackTests
{
    [Fact]
    public void StateTransition_Closed_To_Open_InvokesCallback()
    {
        List<CircuitBreakerAuditEntry> entries = [];
        CircuitBreakerOptions options = new() { FailureThreshold = 5, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate gate = new(
            "cb-test",
            options,
            onAuditEntry: entries.Add);

        for (int i = 0; i < 5; i++)
        {
            gate.RecordFailure();
        }

        CircuitBreakerAuditEntry? lastTransition = entries.LastOrDefault(
            e => e.TransitionType == "StateTransition");

        lastTransition.Should().NotBeNull();
        lastTransition!.FromState.Should().Be("Closed");
        lastTransition.ToState.Should().Be("Open");
    }

    [Fact]
    public void Rejection_WhenOpen_InvokesCallback()
    {
        List<CircuitBreakerAuditEntry> entries = [];
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 60 };
        CircuitBreakerGate gate = new("cb-reject", options, onAuditEntry: entries.Add);
        gate.RecordFailure();

        Action act = () => gate.ThrowIfBroken();

        act.Should().Throw<CircuitBreakerOpenException>();
        entries.Should().Contain(
            e => e.TransitionType == "Rejection" && e.FromState == "Open" && e.ToState == "Open");
    }

    [Fact]
    public void ProbeOutcome_Success_InvokesCallback()
    {
        List<CircuitBreakerAuditEntry> entries = [];
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 30 };
        CircuitBreakerGate gate = new("cb-probe-ok", options, clock.ToFunc(), entries.Add);

        gate.RecordFailure();
        clock.Advance(TimeSpan.FromSeconds(31));
        gate.ThrowIfBroken();
        gate.RecordSuccess();

        entries.Should().Contain(
            e => e.TransitionType == "ProbeOutcome" && e.ProbeOutcome == "success");
    }

    [Fact]
    public void ProbeOutcome_Failure_InvokesCallback()
    {
        List<CircuitBreakerAuditEntry> entries = [];
        MutableUtcClock clock = new(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 30 };
        CircuitBreakerGate gate = new("cb-probe-fail", options, clock.ToFunc(), entries.Add);

        gate.RecordFailure();
        clock.Advance(TimeSpan.FromSeconds(31));
        gate.ThrowIfBroken();
        gate.RecordFailure();

        entries.Should().Contain(
            e => e.TransitionType == "ProbeOutcome" && e.ProbeOutcome == "failure");
    }

    [Fact]
    public void Callback_Null_DoesNotThrow()
    {
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 10 };
        CircuitBreakerGate gate = new("cb-null", options);
        Action actOpen = () => gate.RecordFailure();

        actOpen.Should().NotThrow();

        Action actReject = () => gate.ThrowIfBroken();

        actReject.Should().Throw<CircuitBreakerOpenException>();
    }

    [Fact]
    public void Callback_that_throws_is_swallowed()
    {
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 10 };
        CircuitBreakerGate gate = new(
            "cb-bad-callback",
            options,
            onAuditEntry: _ => throw new InvalidOperationException("boom"));

        Action act = () => gate.RecordFailure();

        act.Should().NotThrow();
    }

    private sealed class MutableUtcClock
    {
        private DateTimeOffset _now;

        public MutableUtcClock(DateTimeOffset start) => _now = start;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);

        public Func<DateTimeOffset> ToFunc() => () => _now;
    }
}
