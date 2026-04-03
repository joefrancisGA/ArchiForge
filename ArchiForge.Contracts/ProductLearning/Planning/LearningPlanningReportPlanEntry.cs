namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>One prioritized improvement plan with evidence references for export.</summary>
public sealed class LearningPlanningReportPlanEntry
{
    public Guid PlanId { get; init; }

    public Guid ThemeId { get; init; }

    public string ThemeTitle { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public int PriorityScore { get; init; }

    public string? PriorityExplanation { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedUtc { get; init; }

    public int ActionStepCount { get; init; }

    public LearningPlanningReportPlanEvidenceBlock Evidence { get; init; } = null!;
}
