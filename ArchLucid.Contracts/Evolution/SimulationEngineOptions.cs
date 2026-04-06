namespace ArchiForge.Contracts.Evolution;

/// <summary>Optional read-profile overrides. Null <see cref="BaselineReadProfile"/> uses <see cref="SimulationReadProfile.StrictReadOnly"/>.</summary>
public sealed class SimulationEngineOptions
{
    /// <summary>First pass (before). When null, <see cref="SimulationReadProfile.StrictReadOnly"/> is used.</summary>
    public SimulationReadProfile? BaselineReadProfile { get; init; }

    /// <summary>Second pass (simulated / after). When null, matches baseline (single read-only pass).</summary>
    public SimulationReadProfile? SimulatedReadProfile { get; init; }
}
