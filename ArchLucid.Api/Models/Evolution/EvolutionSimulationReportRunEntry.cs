namespace ArchiForge.Api.Models.Evolution;

/// <summary>One simulation row in an export: raw outcome, structured shadow, scores, and a human diff summary.</summary>
public sealed class EvolutionSimulationReportRunEntry
{
    public Guid SimulationRunId { get; init; }

    public required string BaselineArchitectureRunId { get; init; }

    public required string EvaluationMode { get; init; }

    public DateTime CompletedUtc { get; init; }

    public bool IsShadowOnly { get; init; }

    public string? OutcomeSchemaVersion { get; init; }

    public string? WarningsJson { get; init; }

    public required string OutcomeJson { get; init; }

    /// <summary>How <see cref="ShadowOutcome"/> was obtained: <c>60R-v2</c>, <c>legacy</c>, <c>none</c>, <c>invalid</c>, <c>unparsed</c>.</summary>
    public required string OutcomeShadowKind { get; init; }

    public EvolutionShadowOutcomeSnapshot? ShadowOutcome { get; init; }

    public EvaluationScoreResponse? EvaluationScore { get; init; }

    public string? EvaluationExplanationSummary { get; init; }

    public IReadOnlyList<string> DiffSummaryLines { get; init; } = [];
}
