using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Contracts.Abstractions.ProductLearning;

/// <summary>
///     Loads scoped feedback rollups, artifact trends, and repeated comments from persistence with noise filters.
/// </summary>
public interface IProductLearningFeedbackAggregationService
{
    /// <summary>Builds a snapshot for the given scope and options (deterministic ordering from repository).</summary>
    Task<ProductLearningAggregationSnapshot> GetSnapshotAsync(
        ProductLearningScope scope,
        ProductLearningTriageOptions options,
        CancellationToken cancellationToken);
}
