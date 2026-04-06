namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// Domain view of a recurring feedback pattern (product-learning theme), before or after persistence.
/// Distinct from <see cref="ProductLearningImprovementThemeRecord"/>, which maps SQL columns.
/// </summary>
public sealed class ImprovementTheme
{
    /// <summary>Stable identifier for the theme (persisted or generated for drafts).</summary>
    public Guid ThemeId { get; init; }

    /// <summary>Short operator-facing label.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Longer narrative: what repeats, where it hurts, and scope of concern.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Count of supporting pilot signals or rolled-up evidence rows (definition supplied by aggregation layer).</summary>
    public int EvidenceCount { get; init; }

    /// <summary>
    /// Workflow or artifact facets touched (e.g. manifest, diagram, export); may be empty when unknown.
    /// </summary>
    public IReadOnlyList<string> AffectedArtifactTypes { get; init; } = [];
    public DateTime FirstSeenUtc { get; init; }
    public DateTime LastSeenUtc { get; init; }
}
