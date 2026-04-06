namespace ArchiForge.Api.Models.Evolution;

/// <summary>Persisted simulation row with optional parsed evaluation (60R-v2 outcome envelope).</summary>
public sealed class EvolutionSimulationRunWithEvaluationResponse
{
    public Guid SimulationRunId { get; init; }

    public required string BaselineArchitectureRunId { get; init; }

    public required string EvaluationMode { get; init; }

    public required string OutcomeJson { get; init; }

    public string? WarningsJson { get; init; }

    public DateTime CompletedUtc { get; init; }

    public bool IsShadowOnly { get; init; }

    public EvaluationScoreResponse? EvaluationScore { get; init; }

    public string? EvaluationExplanationSummary { get; init; }

    public string? OutcomeSchemaVersion { get; init; }
}
