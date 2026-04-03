namespace ArchiForge.Contracts.Evolution;

/// <summary>One shadow evaluation pass for a baseline architecture run (simulation-only row).</summary>
public sealed class EvolutionSimulationRunRecord
{
    public Guid SimulationRunId { get; init; }

    public Guid CandidateChangeSetId { get; init; }

    public string BaselineArchitectureRunId { get; init; } = string.Empty;

    public string EvaluationMode { get; init; } = EvolutionEvaluationModeValues.ReadOnlyArchitectureAnalysis;

    public string OutcomeJson { get; init; } = string.Empty;

    public string? WarningsJson { get; init; }

    public DateTime CompletedUtc { get; init; }

    public bool IsShadowOnly { get; init; } = true;
}
