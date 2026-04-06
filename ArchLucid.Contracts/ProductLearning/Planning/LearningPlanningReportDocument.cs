namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>59R planning export document: top themes, prioritized plans, and evidence references (JSON or markdown source).</summary>
public sealed class LearningPlanningReportDocument
{
    public DateTime GeneratedUtc { get; init; }

    public LearningPlanningReportSummaryBlock Summary { get; init; } = null!;

    public IReadOnlyList<LearningPlanningReportThemeEntry> Themes { get; init; } = [];

    public IReadOnlyList<LearningPlanningReportPlanEntry> Plans { get; init; } = [];
}
