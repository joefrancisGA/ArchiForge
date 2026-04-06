using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ReplayComparisonMetadataResponse
{
    public string ComparisonRecordId { get; set; } = string.Empty;
    public string ComparisonType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ReplayMode { get; set; } = "artifact";
    public bool VerificationPassed { get; set; }
    public string? VerificationMessage { get; set; }
    public DriftAnalysisResponse? DriftAnalysis { get; set; }
    public string? LeftRunId { get; set; }
    public string? RightRunId { get; set; }
    public string? LeftExportRecordId { get; set; }
    public string? RightExportRecordId { get; set; }
    public DateTime? CreatedUtc { get; set; }
    public string? FormatProfile { get; set; }

    /// <summary>When PersistReplay was true: the new comparison record ID created for this replay.</summary>
    public string? PersistedReplayRecordId { get; set; }
}

