namespace ArchiForge.Contracts.ProductLearning;

/// <summary>JSON wrapper when <c>format=markdown</c> on the product-learning triage report endpoint.</summary>
public sealed class ProductLearningReportExportResponse
{
    /// <summary>Always <c>markdown</c> for this response shape.</summary>
    public string Format { get; init; } = "markdown";

    /// <summary>Suggested download filename.</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>Full markdown document.</summary>
    public string Content { get; init; } = string.Empty;
}
