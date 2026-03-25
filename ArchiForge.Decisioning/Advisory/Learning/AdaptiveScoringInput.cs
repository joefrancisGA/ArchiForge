using ArchiForge.Decisioning.Advisory.Services;

namespace ArchiForge.Decisioning.Advisory.Learning;

/// <summary>
/// Inputs for <see cref="IAdaptiveRecommendationScorer.Score"/> aligned with recommendation facets before persistence.
/// </summary>
public class AdaptiveScoringInput
{
    /// <summary>Recommendation category (matches <see cref="RecommendationLearningProfile.CategoryWeights"/> keys when learned).</summary>
    public string Category { get; set; } = null!;

    /// <summary>Urgency band (matches <see cref="RecommendationLearningProfile.UrgencyWeights"/> keys when learned).</summary>
    public string Urgency { get; set; } = null!;

    /// <summary>Optional signal/type facet (matches <see cref="RecommendationLearningProfile.SignalTypeWeights"/> when learned).</summary>
    public string? SignalType
    {
        get; set;
    }

    /// <summary>Heuristic score from <see cref="RecommendationGenerator"/> before learning weights.</summary>
    public int BasePriorityScore
    {
        get; set;
    }
}
