namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Bounded improvement plan under a theme; actions are capped and stored as JSON in SQL.</summary>
public sealed class ProductLearningImprovementPlanRecord
{
    public Guid PlanId { get; init; }

    public Guid TenantId { get; init; }

    public Guid WorkspaceId { get; init; }

    public Guid ProjectId { get; init; }

    public Guid ThemeId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<ProductLearningImprovementPlanActionStep> ActionSteps { get; init; } =
        Array.Empty<ProductLearningImprovementPlanActionStep>();

    /// <summary>Snapshot priority rank (higher = more urgent); formula lives in 59R services, not the DB.</summary>
    public int PriorityScore { get; init; }

    public string? PriorityExplanation { get; init; }

    public string Status { get; init; } = ProductLearningImprovementPlanStatusValues.Proposed;

    public DateTime CreatedUtc { get; init; }

    public string? CreatedByUserId { get; init; }
}
