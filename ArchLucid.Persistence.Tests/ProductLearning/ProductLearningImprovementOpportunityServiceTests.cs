using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Persistence.Tests.ProductLearning;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
[Trait("ChangeSet", "58R")]
public sealed class ProductLearningImprovementOpportunityServiceTests
{
    /// <summary>Rollups above the default <c>MaxImprovementOpportunities</c> cap so ranking honors the limit.</summary>
    private const int SeededFeedbackRollupCountAboveOpportunityCap = 5;

    private static readonly DateTime Early = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime Late = new(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

    [SkippableFact]
    public async Task BuildRankedOpportunitiesAsync_orders_by_bad_score_desc_then_last_seen_then_sort_key()
    {
        ProductLearningScope scope = new()
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };

        FeedbackAggregate weaker = new()
        {
            AggregateKey = "weaker",
            PatternKey = "weaker",
            SubjectTypeOrWorkflowArea = "RunOutput",
            DistinctRunCount = 2,
            TotalSignalCount = 2,
            TrustedCount = 0,
            RejectedCount = 0,
            RevisedCount = 2,
            NeedsFollowUpCount = 0,
            FirstSignalRecordedUtc = Late,
            LastSignalRecordedUtc = Late
        };

        FeedbackAggregate stronger = new()
        {
            AggregateKey = "stronger",
            PatternKey = "stronger",
            SubjectTypeOrWorkflowArea = "RunOutput",
            DistinctRunCount = 2,
            TotalSignalCount = 2,
            TrustedCount = 0,
            RejectedCount = 2,
            RevisedCount = 0,
            NeedsFollowUpCount = 0,
            FirstSignalRecordedUtc = Early,
            LastSignalRecordedUtc = Early
        };

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope,
            SinceUtc = null,
            FeedbackRollups = [weaker, stronger],
            ArtifactTrends = [],
            TopRejectedRevisedRollups = [],
            RepeatedCommentThemes = []
        };

        ProductLearningTriageOptions options = new()
        {
            MinSignalsPerAggregate = 2,
            MinAggregateBadScoreForOpportunity = 2,
            MinNegativeOutcomesOnArtifactTrend = 99,
            MaxImprovementOpportunities = 10
        };

        ProductLearningImprovementOpportunityService svc = new();
        IReadOnlyList<ImprovementOpportunity> ranked =
            await svc.BuildRankedOpportunitiesAsync(snapshot, options, CancellationToken.None);

        ranked.Should().HaveCount(2);
        ranked[0].PriorityRank.Should().Be(1);
        ranked[1].PriorityRank.Should().Be(2);
        ranked[0].PatternKey.Should().Be("stronger");
        ranked[1].PatternKey.Should().Be("weaker");
    }

    [SkippableFact]
    public async Task BuildRankedOpportunitiesAsync_respects_MaxImprovementOpportunities()
    {
        ProductLearningScope scope = new()
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };

        List<FeedbackAggregate> rollups = [];

        for (int i = 0; i < SeededFeedbackRollupCountAboveOpportunityCap; i++)
        {
            rollups.Add(
                new FeedbackAggregate
                {
                    AggregateKey = "p" + i,
                    PatternKey = "p" + i,
                    SubjectTypeOrWorkflowArea = "RunOutput",
                    DistinctRunCount = 2,
                    TotalSignalCount = 2,
                    TrustedCount = 0,
                    RejectedCount = 2,
                    RevisedCount = 0,
                    NeedsFollowUpCount = 0,
                    FirstSignalRecordedUtc = Early,
                    LastSignalRecordedUtc = Early
                });
        }

        ProductLearningAggregationSnapshot snapshot = new()
        {
            Scope = scope,
            SinceUtc = null,
            FeedbackRollups = rollups,
            ArtifactTrends = [],
            TopRejectedRevisedRollups = [],
            RepeatedCommentThemes = []
        };

        ProductLearningTriageOptions options = new()
        {
            MinSignalsPerAggregate = 2,
            MinAggregateBadScoreForOpportunity = 2,
            MaxImprovementOpportunities = 2,
            MinNegativeOutcomesOnArtifactTrend = 99
        };

        ProductLearningImprovementOpportunityService svc = new();
        IReadOnlyList<ImprovementOpportunity> ranked =
            await svc.BuildRankedOpportunitiesAsync(snapshot, options, CancellationToken.None);

        ranked.Should().HaveCount(2);
        ranked.Select(static o => o.PriorityRank).Should().Equal(1, 2);
    }
}
