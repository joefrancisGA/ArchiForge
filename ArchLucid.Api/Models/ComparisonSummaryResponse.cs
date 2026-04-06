using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ComparisonSummaryResponse
{
    public string ComparisonRecordId { get; set; } = string.Empty;
    public string ComparisonType { get; set; } = string.Empty;
    public string Format { get; set; } = "markdown";
    public string Summary { get; set; } = string.Empty;
}

