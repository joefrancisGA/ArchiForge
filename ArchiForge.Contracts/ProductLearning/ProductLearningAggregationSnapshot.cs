namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// Raw and lightly filtered aggregation inputs for a scope (built before opportunity ranking).
/// </summary>
public sealed class ProductLearningAggregationSnapshot
{
    public required ProductLearningScope Scope { get; init; }

    public DateTime? SinceUtc { get; init; }

    public IReadOnlyList<FeedbackAggregate> FeedbackRollups { get; init; } = Array.Empty<FeedbackAggregate>();

    public IReadOnlyList<ArtifactOutcomeTrend> ArtifactTrends { get; init; } = Array.Empty<ArtifactOutcomeTrend>();

    /// <summary>
    /// Reserved: reject/revise-focused rollups are not merged into the dashboard or report pipeline in 58R
    /// (always empty from <see cref="IProductLearningFeedbackAggregationService.GetSnapshotAsync"/> to avoid an extra query).
    /// </summary>
    public IReadOnlyList<FeedbackAggregate> TopRejectedRevisedRollups { get; init; } = Array.Empty<FeedbackAggregate>();

    public IReadOnlyList<RepeatedCommentTheme> RepeatedCommentThemes { get; init; } = Array.Empty<RepeatedCommentTheme>();
}
