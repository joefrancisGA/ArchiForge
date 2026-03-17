namespace ArchiForge.Api.Services;

/// <summary>Single replay operation recorded for diagnostics.</summary>
public sealed class ReplayDiagnosticsEntry
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
