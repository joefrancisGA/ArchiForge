using ArchLucid.Core.Resilience;
using ArchLucid.Core.Time;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace ArchLucid.Core.Tests.Resilience;

[Trait("Suite", "Core")]
public sealed class CircuitBreakerGateOptionsMonitorTests
{
    [Fact]
    public void GateName_returns_constructor_value()
    {
        CircuitBreakerOptions options = new()
        {
            FailureThreshold = 1,
            DurationOfBreakSeconds = 1
        };
        CircuitBreakerGate gate = new("my-gate", options);

        gate.GateName.Should().Be("my-gate");
    }

    [Fact]
    public void CurrentState_reflects_transitions()
    {
        MutableUtcClock clock = new(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
        CircuitBreakerOptions options = new()
        {
            FailureThreshold = 1,
            DurationOfBreakSeconds = 30
        };
        CircuitBreakerGate gate = new("state-gate", options, new DelegateTimeProvider(clock.ToFunc()));

        gate.CurrentState.Should().Be("Closed");

        gate.RecordFailure();
        gate.CurrentState.Should().Be("Open");

        clock.Advance(TimeSpan.FromSeconds(31));
        gate.ThrowIfBroken();
        gate.CurrentState.Should().Be("HalfOpen");

        gate.RecordSuccess();
        gate.CurrentState.Should().Be("Closed");
    }

    [Fact]
    public void OptionsMonitor_reload_changes_failure_threshold()
    {
        CircuitBreakerOptions live = new()
        {
            FailureThreshold = 5,
            DurationOfBreakSeconds = 60
        };
        TestOptionsMonitor monitor = new(live);
        CircuitBreakerGate gate = new("reload-gate", monitor);

        gate.RecordFailure();
        gate.RecordFailure();
        gate.RecordFailure();
        gate.CurrentState.Should().Be("Closed");

        live.FailureThreshold = 3;
        gate.RecordFailure();
        gate.CurrentState.Should().Be("Open");
    }

    private sealed class MutableUtcClock(DateTimeOffset start)
    {
        private DateTimeOffset _now = start;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);

        public Func<DateTimeOffset> ToFunc() => () => _now;
    }

    /// <summary>Stub monitor: <see cref="Get"/> returns the same options instance (mutable for reload simulation).</summary>
    private sealed class TestOptionsMonitor(CircuitBreakerOptions instance) : IOptionsMonitor<CircuitBreakerOptions>
    {
        public CircuitBreakerOptions CurrentValue => instance;

        public CircuitBreakerOptions Get(string? name) => instance;

        public IDisposable OnChange(Action<CircuitBreakerOptions, string> listener) => NoopDisposable.Instance;

        private sealed class NoopDisposable : IDisposable
        {
            internal static readonly NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
            }
        }
    }
}
