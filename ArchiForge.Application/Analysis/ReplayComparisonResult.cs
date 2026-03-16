namespace ArchiForge.Application.Analysis;

public sealed class ReplayComparisonResult
{
    public string ComparisonRecordId { get; set; } = string.Empty;

    public string ComparisonType { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string? Content { get; set; }

    public byte[]? BinaryContent { get; set; }

    /// <summary>Replay mode used: artifact, regenerate, verify.</summary>
    public string ReplayMode { get; set; } = "artifact";

    /// <summary>When replay mode is Verify: true if regenerated payload matched stored payload.</summary>
    public bool VerificationPassed { get; set; }

    /// <summary>When replay mode is Verify: message describing verification outcome.</summary>
    public string? VerificationMessage { get; set; }

    /// <summary>When replay mode is Verify and drift detected: structured drift analysis.</summary>
    public DriftAnalysisResult? DriftAnalysis { get; set; }
}

