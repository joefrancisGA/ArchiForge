using ArchiForge.Decisioning.Advisory.Services;

namespace ArchiForge.Decisioning.Advisory.Learning;

/// <summary>
/// Adjusts a base recommendation priority using optional weights from a <see cref="RecommendationLearningProfile"/>.
/// </summary>
/// <remarks>
/// Registered singleton in API composition. Primary caller: <see cref="RecommendationGenerator"/>.
/// </remarks>
public interface IAdaptiveRecommendationScorer
{
    /// <summary>
    /// Multiplies <see cref="AdaptiveScoringInput.BasePriorityScore"/> by category, urgency, and optional signal-type weights when present in <paramref name="profile"/>.
    /// </summary>
    /// <param name="input">Facet labels and non-adapted score from the generator.</param>
    /// <param name="profile">Learning snapshot; when <see langword="null"/>, adapted score equals the base score.</param>
    /// <returns>Base and adapted integers plus per-facet weights and human-readable <see cref="AdaptiveScoringResult.Notes"/>.</returns>
    AdaptiveScoringResult Score(
        AdaptiveScoringInput input,
        RecommendationLearningProfile? profile);
}
