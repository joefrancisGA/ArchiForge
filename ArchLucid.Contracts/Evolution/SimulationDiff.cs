namespace ArchiForge.Contracts.Evolution;

/// <summary>Explainable diff payload between baseline and simulated evaluation views (structure only; no comparison logic here).</summary>
public sealed class SimulationDiff
{
    public string Summary { get; init; } = string.Empty;

    /// <summary>Opaque, JSON-serializable detail for auditors (e.g. manifest or metric deltas).</summary>
    public string? DetailJson { get; init; }
}
