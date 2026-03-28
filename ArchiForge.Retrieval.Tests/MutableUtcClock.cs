namespace ArchiForge.Retrieval.Tests;

/// <summary>Mutable clock for deterministic <see cref="ArchiForge.Core.Resilience.CircuitBreakerGate"/> tests.</summary>
internal sealed class MutableUtcClock(DateTimeOffset start)
{
    private DateTimeOffset _t = start;

    public void Advance(TimeSpan delta)
    {
        _t += delta;
    }

    public Func<DateTimeOffset> ToFunc()
    {
        return () => _t;
    }
}
