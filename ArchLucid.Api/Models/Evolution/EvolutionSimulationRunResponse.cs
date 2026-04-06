namespace ArchiForge.Api.Models.Evolution;

/// <summary>One persisted shadow evaluation row.</summary>
public sealed class EvolutionSimulationRunResponse
{
    public Guid SimulationRunId { get; init; }

    public required string BaselineArchitectureRunId { get; init; }

    public required string EvaluationMode { get; init; }

    public required string OutcomeJson { get; init; }

    public string? WarningsJson { get; init; }

    public DateTime CompletedUtc { get; init; }

    public bool IsShadowOnly { get; init; }
}
