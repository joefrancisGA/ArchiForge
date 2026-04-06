namespace ArchiForge.Contracts.Evolution;

/// <summary>Bounded excerpts from read-only analysis outputs (before/after passes) for human review.</summary>
public sealed class SimulationArtifactsSnapshot
{
    public string BaselineManifestVersion { get; init; } = string.Empty;

    public string SimulatedManifestVersion { get; init; } = string.Empty;

    public int BaselineSummaryLength { get; init; }

    public int SimulatedSummaryLength { get; init; }

    public string? BaselineSummaryPreview { get; init; }

    public string? SimulatedSummaryPreview { get; init; }
}
