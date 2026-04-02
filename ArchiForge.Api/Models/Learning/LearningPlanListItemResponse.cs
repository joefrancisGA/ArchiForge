namespace ArchiForge.Api.Models.Learning;

/// <summary>Improvement plan summary for list views; includes theme-level evidence volume when the theme is resolved.</summary>
public sealed class LearningPlanListItemResponse
{
    public Guid PlanId { get; init; }

    public Guid ThemeId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    /// <summary>Combined priority rank from 59R prioritization (snapshot on plan row).</summary>
    public int PriorityScore { get; init; }

    public string? PriorityExplanation { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedUtc { get; init; }

    /// <summary>Evidence signal count from the parent theme (same metric as <see cref="LearningThemeResponse.EvidenceSignalCount"/>).</summary>
    public int? ThemeEvidenceSignalCount { get; init; }
}
