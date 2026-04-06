namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// Repeated artifact-level outcomes over a time window (trust / revise / reject / follow-up mix).
/// Analytics layer assigns <see cref="TrendKey"/> and bucket boundaries — this type is data-only.
/// </summary>
public sealed class ArtifactOutcomeTrend
{
    /// <summary>Stable identifier for this trend row in dashboards and exports.</summary>
    public string TrendKey { get; init; } = string.Empty;

    /// <summary>Artifact facet, logical name, or workflow area (e.g. manifest artifact kind, export format).</summary>
    public string ArtifactTypeOrHint { get; init; } = string.Empty;

    /// <summary>Optional display label for the window (e.g. "Last 7 days") — set by reporting, not inferred here.</summary>
    public string? WindowLabel { get; init; }

    /// <summary>Signals interpreted as positive acceptance/trust (maps to <see cref="ProductLearningDispositionValues.Trusted"/> in 58R pipelines).</summary>
    public int AcceptedOrTrustedCount { get; init; }
    public int RevisionCount { get; init; }
    public int RejectionCount { get; init; }
    public int NeedsFollowUpCount { get; init; }
    public int DistinctRunCount { get; init; }
    public double? AverageTrustScore { get; init; }
    public double? AverageUsefulnessScore { get; init; }

    /// <summary>Compact indicator of a recurring note/theme when the pipeline can derive one safely.</summary>
    public string? RepeatedThemeIndicator { get; init; }
    public DateTime FirstSeenUtc { get; init; }
    public DateTime LastSeenUtc { get; init; }
}
