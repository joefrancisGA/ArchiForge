using ArchLucid.Contracts.ProductLearning.Planning;

namespace ArchLucid.Contracts.Abstractions.ProductLearning.Planning;

/// <summary>
///     Ranks improvement plans with deterministic, batch-normalized weighted scores (no LLM).
/// </summary>
public interface IImprovementPlanPrioritizationService
{
    /// <summary>
    ///     Returns plans sorted by <see cref="ImprovementPlan.PriorityScore" /> descending, then
    ///     <see cref="ImprovementPlan.PlanId" />.
    ///     Each plan is cloned with updated <see cref="ImprovementPlan.PriorityScore" /> and
    ///     <see cref="ImprovementPlan.PrioritizationExplanation" />.
    /// </summary>
    Task<IReadOnlyList<ImprovementPlan>> RankPlansAsync(
        IReadOnlyList<ImprovementPlanScoreInput> items,
        ImprovementPlanPrioritizationWeights weights,
        CancellationToken cancellationToken);
}
