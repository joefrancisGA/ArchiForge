namespace ArchiForge.Contracts.Evolution;

/// <summary>Options for simulation evaluation (determinism has operational side effects when live).</summary>
public sealed class SimulationEvaluationOptions
{
    /// <summary>
    /// When <c>true</c>, invokes the determinism check pipeline using <see cref="BaselineArchitectureRunIdForDeterminism"/>.
    /// Replays may create architecture run rows (even when manifest commits are disabled); use only in controlled environments.
    /// </summary>
    public bool InvokeLiveDeterminismCheck { get; init; }

    /// <summary>Required when <see cref="InvokeLiveDeterminismCheck"/> is <c>true</c>.</summary>
    public string? BaselineArchitectureRunIdForDeterminism { get; init; }

    /// <summary>Passed to determinism check when live (minimum 2 enforced by service).</summary>
    public int DeterminismIterations { get; init; } = 3;
}
