namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// Raw and lightly filtered aggregation inputs for a scope (built before opportunity ranking).
/// </summary>
public sealed class ProductLearningAggregationSnapshot
{
    public required ProductLearningScope Scope { get; init; }
    public DateTime? SinceUtc { get; init; }
    public IReadOnlyList<FeedbackAggregate> FeedbackRollups { get; init; } = [];
    public IReadOnlyList<ArtifactOutcomeTrend> ArtifactTrends { get; init; } = [];

    /// <summary>
    /// Reserved: reject/revise-focused rollups are not merged into the dashboard or report pipeline in 58R
    /// (always empty from <see cref="IProductLearningFeedbackAggregationService.GetSnapshotAsync"/> to avoid an extra query).
    /// </summary>
    public IReadOnlyList<FeedbackAggregate> TopRejectedRevisedRollups { get; init; } = [];
    public IReadOnlyList<RepeatedCommentTheme> RepeatedCommentThemes { get; init; } = [];
}
