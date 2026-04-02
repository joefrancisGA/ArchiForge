namespace ArchiForge.Api.Models.Learning;

/// <summary>Full improvement plan for detail views, including action steps and link-based evidence counts.</summary>
public sealed class LearningPlanDetailResponse
{
    public Guid PlanId { get; init; }

    public Guid ThemeId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public int PriorityScore { get; init; }

    public string? PriorityExplanation { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedUtc { get; init; }

    public string? CreatedByUserId { get; init; }

    public IReadOnlyList<LearningPlanStepResponse> ActionSteps { get; init; } = [];

    public LearningPlanEvidenceCountsResponse EvidenceCounts { get; init; } = new();

    /// <summary>Parent theme snapshot when available (evidence counts and labels).</summary>
    public LearningThemeResponse? Theme { get; init; }
}
