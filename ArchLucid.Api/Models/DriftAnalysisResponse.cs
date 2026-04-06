using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class DriftAnalysisResponse
{
    public bool DriftDetected { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<DriftItemResponse> Items { get; set; } = [];
}
