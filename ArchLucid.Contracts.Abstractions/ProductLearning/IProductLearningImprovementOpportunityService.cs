using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Contracts.Abstractions.ProductLearning;

/// <summary>
///     Ranks improvement candidates from an aggregation snapshot using explicit scoring (no LLM).
/// </summary>
public interface IProductLearningImprovementOpportunityService
{
    /// <summary>
    ///     Produces a deterministic ranked list of <see cref="ImprovementOpportunity" /> (new Guids per call).
    /// </summary>
    Task<IReadOnlyList<ImprovementOpportunity>> BuildRankedOpportunitiesAsync(
        ProductLearningAggregationSnapshot snapshot,
        ProductLearningTriageOptions options,
        CancellationToken cancellationToken);
}
