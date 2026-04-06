namespace ArchiForge.Api.Models.Learning;

/// <summary>Cross-cutting 59R learning/planning KPIs for dashboard shells.</summary>
public sealed class LearningSummaryResponse
{
    public DateTime GeneratedUtc { get; init; }

    public int ThemeCount { get; init; }

    public int PlanCount { get; init; }

    /// <summary>Sum of <see cref="LearningThemeResponse.EvidenceSignalCount"/> across themes in scope (same cap as themes list).</summary>
    public int TotalThemeEvidenceSignals { get; init; }

    /// <summary>Maximum <see cref="LearningPlanListItemResponse.PriorityScore"/> among plans in scope, or null when there are no plans.</summary>
    public int? MaxPlanPriorityScore { get; init; }

    /// <summary>Sum of linked pilot signals across all plans in scope (explicit plan–signal links).</summary>
    public int TotalLinkedSignalsAcrossPlans { get; init; }
}
