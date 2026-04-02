using ArchiForge.Contracts.ProductLearning;

namespace ArchiForge.Persistence.ProductLearning;

/// <inheritdoc />
public sealed class ProductLearningFeedbackAggregationService(IProductLearningPilotSignalRepository repository)
    : IProductLearningFeedbackAggregationService
{
    public async Task<ProductLearningAggregationSnapshot> GetSnapshotAsync(
        ProductLearningScope scope,
        ProductLearningTriageOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(options);

        DateTime? sinceUtc = options.SinceUtc;
        int maxRollups = options.MaxFeedbackRollups < 1 ? 1 : Math.Min(options.MaxFeedbackRollups, 500);
        int maxTrends = options.MaxArtifactTrends < 1 ? 1 : Math.Min(options.MaxArtifactTrends, 500);
        int minComment = options.MinCommentThemeOccurrences < 1 ? 1 : options.MinCommentThemeOccurrences;
        int maxThemes = options.MaxCommentThemes < 1 ? 1 : Math.Min(options.MaxCommentThemes, 200);

        IReadOnlyList<FeedbackAggregate> rawRollups = await repository.ListRunFeedbackAggregatesAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            sinceUtc,
            maxRollups,
            cancellationToken);

        IReadOnlyList<FeedbackAggregate> rollups = rawRollups
            .Where(a => a.TotalSignalCount >= options.MinSignalsPerAggregate)
            .ToList();

        IReadOnlyList<ArtifactOutcomeTrend> rawTrends = await repository.ListArtifactOutcomeTrendsAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            sinceUtc,
            options.TrendWindowLabel,
            maxTrends,
            cancellationToken);

        IReadOnlyList<ArtifactOutcomeTrend> trends = rawTrends
            .Where(t =>
                ProductLearningOpportunityScoring.TotalTrendSignals(t) >= options.MinSignalsPerAggregate &&
                ProductLearningOpportunityScoring.ComputeTrendNegativeMass(t) >= options.MinNegativeOutcomesOnArtifactTrend)
            .ToList();

        IReadOnlyList<RepeatedCommentTheme> themes = await repository.ListRepeatedCommentThemesAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            sinceUtc,
            minComment,
            maxThemes,
            cancellationToken);

        return new ProductLearningAggregationSnapshot
        {
            Scope = scope,
            SinceUtc = sinceUtc,
            FeedbackRollups = rollups,
            ArtifactTrends = trends,
            TopRejectedRevisedRollups = Array.Empty<FeedbackAggregate>(),
            RepeatedCommentThemes = themes,
        };
    }
}
