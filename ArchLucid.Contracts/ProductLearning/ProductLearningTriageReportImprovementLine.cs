namespace ArchiForge.Contracts.ProductLearning;

/// <summary>One improvement opportunity line for exports (trimmed summary).</summary>
public sealed class ProductLearningTriageReportImprovementLine
{
    public string Title { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Area { get; init; } = string.Empty;

    /// <summary>Truncated for readability in reports.</summary>
    public string Summary { get; init; } = string.Empty;
}
