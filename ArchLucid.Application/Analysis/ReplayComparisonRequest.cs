namespace ArchiForge.Application.Analysis;

public sealed class ReplayComparisonRequest
{
    public string ComparisonRecordId { get; set; } = string.Empty;
    public string Format { get; set; } = "markdown";

    /// <summary>Replay mode: artifact (default), regenerate, verify.</summary>
    public string ReplayMode { get; set; } = "artifact";

    /// <summary>Export profile for end-to-end comparison: default, short, detailed, executive.</summary>
    public string? Profile { get; set; }

    /// <summary>When true, persist this replay as a new comparison record (idempotent re-persist).</summary>
    public bool PersistReplay { get; set; }
}

