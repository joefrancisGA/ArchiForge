using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ConsultingDocxExportResponse
{
    public string RunId { get; set; } = string.Empty;
    public string SelectedProfileName { get; set; } = string.Empty;
    public string SelectedProfileDisplayName { get; set; } = string.Empty;
    public bool WasAutoSelected { get; set; }
    public string ResolutionReason { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

