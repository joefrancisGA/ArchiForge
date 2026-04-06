namespace ArchiForge.Contracts.ProductLearning;

/// <summary>Artifact-level outcome rollups for trend visualization.</summary>
public sealed class ProductLearningArtifactOutcomeTrendsResponse
{
    public DateTime GeneratedUtc { get; init; }
    public IReadOnlyList<ArtifactOutcomeTrend> Trends { get; init; } = [];
}
