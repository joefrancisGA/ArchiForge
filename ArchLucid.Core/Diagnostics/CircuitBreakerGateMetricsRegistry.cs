using ArchLucid.Core.Resilience;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Holds strong references to registered <see cref="CircuitBreakerGate" /> instances for observable gauges (small
///     fixed cardinality — OpenAI completion / embedding gates only).
/// </summary>
public static class CircuitBreakerGateMetricsRegistry
{
    private static readonly object Sync = new();

    private static readonly Dictionary<string, CircuitBreakerGate> Gates = new(StringComparer.Ordinal);

    /// <summary>Idempotent registration when the host constructs keyed gates.</summary>
    public static void Register(CircuitBreakerGate gate)
    {
        ArgumentNullException.ThrowIfNull(gate);

        lock (Sync)

            Gates[gate.GateName] = gate;
    }

    /// <summary>Snapshot gate names and textual states for OTel observable callbacks.</summary>
    public static IReadOnlyList<(string GateName, string State)> SnapshotStates()
    {
        lock (Sync)

            return Gates.Values.Select(static g => (g.GateName, g.CurrentState)).ToArray();
    }
}
