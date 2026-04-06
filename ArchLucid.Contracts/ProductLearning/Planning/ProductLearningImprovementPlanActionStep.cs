namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>One bounded, reviewable action inside a plan (serialized to <c>BoundedActionsJson</c>).</summary>
public sealed class ProductLearningImprovementPlanActionStep
{
    /// <summary>1-based order; duplicates are rejected at persistence.</summary>
    public int Ordinal { get; init; }

    /// <summary>Stable category for reporting (e.g. Investigate, ClarifyPolicy, UX).</summary>
    public string ActionType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? AcceptanceCriteria { get; init; }
}
