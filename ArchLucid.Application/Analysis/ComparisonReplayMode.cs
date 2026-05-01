namespace ArchLucid.Application.Analysis;

/// <summary>
///     How to replay a persisted comparison record.
/// </summary>
public enum ComparisonReplayMode
{
    /// <summary>Use stored comparison payload only (default).</summary>
    ArtifactReplay,

    /// <summary>Re-run the comparison from source artifacts.</summary>
    Regenerate,

    /// <summary>Re-run comparison and confirm it matches stored payload (deterministic verification).</summary>
    Verify
}
