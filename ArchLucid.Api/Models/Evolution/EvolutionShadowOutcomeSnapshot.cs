namespace ArchiForge.Api.Models.Evolution;

/// <summary>Shadow slice of a persisted simulation outcome (60R-v2 envelope or legacy flat JSON).</summary>
public sealed class EvolutionShadowOutcomeSnapshot
{
    public string? Error { get; init; }

    public string ArchitectureRunId { get; init; } = string.Empty;

    public string EvaluationMode { get; init; } = string.Empty;

    public string? RunStatus { get; init; }

    public string? ManifestVersion { get; init; }

    public bool HasManifest { get; init; }

    public int SummaryLength { get; init; }

    public int WarningCount { get; init; }
}
