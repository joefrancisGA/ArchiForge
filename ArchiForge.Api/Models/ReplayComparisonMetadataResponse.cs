namespace ArchiForge.Api.Models;

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
}

