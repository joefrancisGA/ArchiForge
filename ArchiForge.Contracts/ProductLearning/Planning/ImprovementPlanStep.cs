namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// A single bounded, human-reviewable action in an improvement plan (not executable code).
/// </summary>
public sealed class ImprovementPlanStep
{
    /// <summary>1-based display order.</summary>
    public int Ordinal { get; init; }

    /// <summary>Short label (e.g. “Clarify acceptance criteria”).</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>What to do, in plain language.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Stable category for reporting (e.g. Investigate, Policy, UX).</summary>
    public string ActionType { get; init; } = string.Empty;
}
