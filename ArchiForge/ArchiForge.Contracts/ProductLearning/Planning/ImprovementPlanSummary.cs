namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// Lightweight list/detail header for API and UI without full <see cref="ImprovementPlan"/> payloads.
/// </summary>
public sealed class ImprovementPlanSummary
{
    public Guid PlanId { get; init; }

    public Guid ThemeId { get; init; }

    public string Title { get; init; } = string.Empty;

    public int PriorityScore { get; init; }

    public DateTime CreatedUtc { get; init; }

    /// <summary>Number of steps in <see cref="ImprovementPlan.ProposedChanges"/> when known.</summary>
    public int ProposedChangeCount { get; init; }

    /// <summary>Aligns with <see cref="ProductLearningImprovementPlanStatusValues"/> when sourced from persistence.</summary>
    public string? Status { get; init; }
}
