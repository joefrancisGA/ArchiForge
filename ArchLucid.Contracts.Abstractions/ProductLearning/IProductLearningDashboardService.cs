using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Contracts.Abstractions.ProductLearning;

/// <summary>
/// Composes the operator product-learning dashboard: counts, snapshot slices, opportunities, and triage queue.
/// </summary>
public interface IProductLearningDashboardService
{
    Task<LearningDashboardSummary> GetDashboardSummaryAsync(
        ProductLearningScope scope,
        ProductLearningTriageOptions options,
        CancellationToken cancellationToken);
}
