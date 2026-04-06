namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// Structured, bounded improvement proposal for a theme. Distinct from <see cref="ProductLearningImprovementPlanRecord"/> (SQL shape).
/// </summary>
public sealed class ImprovementPlan
{
    public Guid PlanId { get; init; }
    public Guid ThemeId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>Ordered human actions (bounded list; max enforced by services/persistence).</summary>
    public IReadOnlyList<ImprovementPlanStep> ProposedChanges { get; init; } = [];
    public int PriorityScore { get; init; }
    public int FrequencyScore { get; init; }
    public int SeverityScore { get; init; }
    public double TrustImpactScore { get; init; }
    public DateTime CreatedUtc { get; init; }

    /// <summary>
    /// Filled by <see cref="IImprovementPlanPrioritizationService"/> with a short deterministic breakdown (weights + normalized axes).
    /// </summary>
    public string? PrioritizationExplanation { get; init; }
}
