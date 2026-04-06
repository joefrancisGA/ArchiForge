namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>One improvement theme row in a planning report (read-only export slice).</summary>
public sealed class LearningPlanningReportThemeEntry
{
    public Guid ThemeId { get; init; }

    public string ThemeKey { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string SeverityBand { get; init; } = string.Empty;

    public int EvidenceSignalCount { get; init; }

    public int DistinctRunCount { get; init; }

    public string Status { get; init; } = string.Empty;
}
