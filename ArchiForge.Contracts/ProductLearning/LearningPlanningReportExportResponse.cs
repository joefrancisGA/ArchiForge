namespace ArchiForge.Contracts.ProductLearning;

/// <summary>JSON wrapper when <c>format=markdown</c> on the 59R learning planning report endpoint.</summary>
public sealed class LearningPlanningReportExportResponse
{
    public string Format { get; init; } = "markdown";

    public string FileName { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;
}
