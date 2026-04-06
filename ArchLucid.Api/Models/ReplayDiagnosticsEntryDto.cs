using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

/// <summary>A single replay execution entry within a <see cref="ReplayDiagnosticsResponse"/>.</summary>
[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ReplayDiagnosticsEntryDto
{
    public DateTime TimestampUtc { get; set; }
    public string ComparisonRecordId { get; set; } = string.Empty;
    public string ComparisonType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string ReplayMode { get; set; } = string.Empty;
    public bool PersistReplay { get; set; }
    public long DurationMs { get; set; }
    public bool Success { get; set; }
    public bool? VerificationPassed { get; set; }
    public string? PersistedReplayRecordId { get; set; }
    public string? ErrorMessage { get; set; }
    public bool MetadataOnly { get; set; }
}
