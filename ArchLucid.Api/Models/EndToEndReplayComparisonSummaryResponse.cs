using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class EndToEndReplayComparisonSummaryResponse
{
    public string Format { get; set; } = "markdown";
    public string Summary { get; set; } = string.Empty;
}

