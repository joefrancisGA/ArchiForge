using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class UpdateComparisonRecordRequest
{
    /// <summary>Optional short label (e.g. release-1.2, incident-42). Pass null to leave unchanged; pass empty string to clear.</summary>
    public string? Label { get; set; }

    /// <summary>Optional tags. Pass null to leave unchanged; pass empty list to clear all tags.</summary>
    public List<string>? Tags { get; set; }
}
