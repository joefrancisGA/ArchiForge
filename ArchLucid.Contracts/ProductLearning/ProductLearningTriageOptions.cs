namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// Thresholds for deterministic triage (tune per environment; defaults favor low noise over recall).
/// </summary>
public sealed class ProductLearningTriageOptions
{
    /// <summary>UTC lower bound for signals included in rollups (null = all time).</summary>
    public DateTime? SinceUtc { get; init; }

    /// <summary>Label applied to <see cref="ArtifactOutcomeTrend.WindowLabel"/> (e.g. "Last 30 days").</summary>
    public string? TrendWindowLabel { get; init; }

    /// <summary>Minimum signals in a feedback rollup before it is surfaced in dashboard slices.</summary>
    public int MinSignalsPerAggregate { get; init; } = 2;

    /// <summary>Minimum rejected+follow-up+revised outcomes on an artifact trend before it becomes an opportunity.</summary>
    public int MinNegativeOutcomesOnArtifactTrend { get; init; } = 2;

    /// <summary>Weighted bad-score floor for rollup-derived opportunities (see dashboard service docs).</summary>
    public int MinAggregateBadScoreForOpportunity { get; init; } = 4;

    /// <summary>Minimum identical comment-prefix occurrences before a theme appears in snapshot and queue.</summary>
    public int MinCommentThemeOccurrences { get; init; } = 2;

    /// <summary>Occurrences required before a comment theme also gets a <see cref="TriageQueueItem"/>.</summary>
    public int MinCommentOccurrencesForTriageQueue { get; init; } = 4;

    /// <summary>Cap on improvement opportunities after ranking.</summary>
    public int MaxImprovementOpportunities { get; init; } = 30;

    /// <summary>Cap on triage queue rows after merge/sort.</summary>
    public int MaxTriageQueueItems { get; init; } = 25;

    /// <summary>Max rollups stored on the dashboard (repository cap).</summary>
    public int MaxFeedbackRollups { get; init; } = 200;

    /// <summary>Max artifact trends fetched.</summary>
    public int MaxArtifactTrends { get; init; } = 100;

    /// <summary>
    /// Take size for reject/revised rollup queries on the repository. Not used by
    /// <see cref="IProductLearningFeedbackAggregationService.GetSnapshotAsync"/> in 58R (property reserved for a future panel or caller).
    /// </summary>
    public int TopRejectedRevisedTake { get; init; } = 25;

    /// <summary>Max repeated-comment themes.</summary>
    public int MaxCommentThemes { get; init; } = 20;
}
