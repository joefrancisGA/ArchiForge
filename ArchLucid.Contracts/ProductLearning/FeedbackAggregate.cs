namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// Rolled-up pilot/product feedback for a stable key (typically <see cref="PatternKey"/> or a composite scope key).
/// Populated by analytics queries — no behavior on this type.
/// </summary>
public sealed class FeedbackAggregate
{
    /// <summary>Stable key used for grouping and dashboard identity (e.g. normalized pattern key).</summary>
    public string AggregateKey { get; init; } = string.Empty;

    /// <summary>Optional normalized pattern bucket; may match <see cref="ProductLearningPilotSignalRecord.PatternKey"/>.</summary>
    public string? PatternKey { get; init; }

    /// <summary>Subject facet or coarse workflow area (e.g. <see cref="ProductLearningSubjectTypeValues"/> tokens).</summary>
    public string SubjectTypeOrWorkflowArea { get; init; } = string.Empty;

    /// <summary>Count of distinct architecture runs referenced by underlying signals in this aggregate.</summary>
    public int DistinctRunCount { get; init; }

    /// <summary>Total signals contributing to this aggregate.</summary>
    public int TotalSignalCount { get; init; }
    public int TrustedCount { get; init; }
    public int RejectedCount { get; init; }
    public int RevisedCount { get; init; }
    public int NeedsFollowUpCount { get; init; }

    /// <summary>Optional mean trust score when source data includes numeric ratings (else null).</summary>
    public double? AverageTrustScore { get; init; }

    /// <summary>Optional mean usefulness score when source data includes numeric ratings (else null).</summary>
    public double? AverageUsefulnessScore { get; init; }

    /// <summary>Short hint for repeated wording or theme (e.g. trimmed common prefix), not a full NLP summary.</summary>
    public string? DominantThemeHint { get; init; }
    public DateTime FirstSignalRecordedUtc { get; init; }
    public DateTime LastSignalRecordedUtc { get; init; }
}
