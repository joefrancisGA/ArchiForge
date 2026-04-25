using ArchLucid.Core.Resilience;
using ArchLucid.Core.Time;

using FluentAssertions;

namespace ArchLucid.Core.Tests.Resilience;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class CircuitBreakerGateAuditCallbackTests
{
    [Fact]
    public void State_transition_audit_callback_that_throws_does_not_prevent_open_transition()
    {
        MutableUtcClock clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions options = new() { FailureThreshold = 1, DurationOfBreakSeconds = 10 };
        int auditCalls = 0;
        CircuitBreakerGate gate = new(
            "audit-throw-gate",
            options,
            new DelegateTimeProvider(clock.ToFunc()),
            _ =>
            {
                auditCalls++;
                throw new InvalidOperationException("audit sink offline");
            });

        gate.RecordFailure();

        gate.CurrentState.Should().Be("Open");
        auditCalls.Should().BeGreaterThan(0);
    }

    private sealed class MutableUtcClock
    {
        private DateTimeOffset _now;

        public MutableUtcClock(DateTimeOffset start)
        {
            _now = start;
        }

        public void Advance(TimeSpan delta)
        {
            _now = _now.Add(delta);
        }

        public Func<DateTimeOffset> ToFunc()
        {
            return () => _now;
        }
    }
}
