namespace ArchiForge.Contracts.Evolution;

/// <summary>Outcome of shadow evaluation for a single baseline architecture run (read-side simulation).</summary>
public sealed class SimulationResult
{
    public string BaselineArchitectureRunId { get; init; } = string.Empty;

    public EvaluationScore? Scores { get; init; }

    public SimulationDiff? Diff { get; init; }

    /// <summary>Captured read-side outputs (bounded previews) from baseline and simulated analysis passes.</summary>
    public SimulationArtifactsSnapshot? Artifacts { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public DateTime CompletedUtc { get; init; }
}
