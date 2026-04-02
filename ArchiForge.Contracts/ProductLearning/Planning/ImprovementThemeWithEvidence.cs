namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Extracted theme plus capped example evidence and an operator-readable grouping rationale.</summary>
public sealed class ImprovementThemeWithEvidence
{
    public ImprovementTheme Theme { get; init; } = null!;

    public IReadOnlyList<ImprovementThemeEvidence> ExampleEvidence { get; init; } = Array.Empty<ImprovementThemeEvidence>();

    /// <summary>Stable logical key used for deduplication and persistence <c>ThemeKey</c> mapping.</summary>
    public string CanonicalKey { get; init; } = string.Empty;

    /// <summary>Plain-language explanation of which heuristic produced the theme.</summary>
    public string GroupingExplanation { get; init; } = string.Empty;
}
