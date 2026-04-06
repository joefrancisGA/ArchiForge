using ArchiForge.Decisioning.Advisory.Workflow;

namespace ArchiForge.Decisioning.Advisory.Learning;

/// <summary>
/// Pure function from recommendation rows to a <see cref="RecommendationLearningProfile"/> (counts, rates, weights, notes).
/// </summary>
public interface IRecommendationLearningAnalyzer
{
    /// <summary>Aggregates <paramref name="recommendations"/> into a new profile DTO (caller persists).</summary>
    RecommendationLearningProfile BuildProfile(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IReadOnlyList<RecommendationRecord> recommendations);
}
