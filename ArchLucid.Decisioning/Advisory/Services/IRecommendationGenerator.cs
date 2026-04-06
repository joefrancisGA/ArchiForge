using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Models;

namespace ArchiForge.Decisioning.Advisory.Services;

/// <summary>
/// Turns analyzed <see cref="ImprovementSignal"/> values into prioritized <see cref="ImprovementRecommendation"/> items, optionally adjusted by a learning profile.
/// </summary>
/// <remarks>
/// Default implementation: <see cref="RecommendationGenerator"/> (singleton in API composition). Called from <see cref="ImprovementAdvisorService"/> after <see cref="ArchiForge.Decisioning.Advisory.Analysis.IImprovementSignalAnalyzer"/> runs.
/// </remarks>
public interface IRecommendationGenerator
{
    /// <summary>
    /// Produces ordered recommendations (highest <see cref="ImprovementRecommendation.PriorityScore"/> first, then title).
    /// </summary>
    /// <param name="signals">Non-empty list typically from manifest/findings (and comparison) analysis.</param>
    /// <param name="profile">Optional; when present, <see cref="IAdaptiveRecommendationScorer"/> adjusts priority scores.</param>
    /// <returns>Zero or more recommendations; never <c>null</c>.</returns>
    IReadOnlyList<ImprovementRecommendation> Generate(
        IReadOnlyList<ImprovementSignal> signals,
        RecommendationLearningProfile? profile = null);
}
