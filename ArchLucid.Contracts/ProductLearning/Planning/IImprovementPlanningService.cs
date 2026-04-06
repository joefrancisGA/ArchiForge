namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// Builds bounded, human-reviewable <see cref="ImprovementPlan"/> rows from extracted themes (rule-based; no LLM).
/// </summary>
public interface IImprovementPlanningService
{
    /// <summary>
    /// One plan per theme, ordered by theme id then canonical key. Scores are deterministic inputs for prioritization.
    /// </summary>
    Task<IReadOnlyList<ImprovementPlan>> BuildPlansAsync(
        IReadOnlyList<ImprovementThemeWithEvidence> themes,
        ImprovementPlanningOptions options,
        CancellationToken cancellationToken);
}
