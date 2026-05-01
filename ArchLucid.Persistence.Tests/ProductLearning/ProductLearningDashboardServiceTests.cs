using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Persistence.Tests.ProductLearning;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
[Trait("ChangeSet", "58R")]
public sealed class ProductLearningDashboardServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [SkippableFact]
    public async Task GetDashboardSummaryAsync_ranks_opportunities_and_counts_signals()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Rejected, "bad-pattern", "run-1"),
            CancellationToken.None);

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.NeedsFollowUp, "bad-pattern", "run-2"),
            CancellationToken.None);

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Trusted, "ok-pattern", "run-3"),
            CancellationToken.None);

        ProductLearningScope scope = new()
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId
        };

        ProductLearningTriageOptions options = new()
        {
            MinSignalsPerAggregate = 2,
            MinAggregateBadScoreForOpportunity = 2,
            MinNegativeOutcomesOnArtifactTrend = 99,
            MaxImprovementOpportunities = 10,
            MaxTriageQueueItems = 10,
            MinCommentOccurrencesForTriageQueue = 99
        };

        ProductLearningFeedbackAggregationService aggregation = new(repo);
        ProductLearningImprovementOpportunityService opportunities = new();
        ProductLearningDashboardService dashboard = new(repo, aggregation, opportunities);

        LearningDashboardSummary summary =
            await dashboard.GetDashboardSummaryAsync(scope, options, CancellationToken.None);

        summary.TotalSignalsInScope.Should().Be(3);
        summary.DistinctRunsTouched.Should().Be(3);
        summary.Opportunities.Should().NotBeEmpty();
        summary.TriageQueue.Should().NotBeEmpty();
        summary.TriageQueue[0].PriorityRank.Should().Be(1);
        summary.SummaryNotes.Should().NotBeEmpty();
    }

    [SkippableFact]
    public async Task CountSignalsInScopeAsync_matches_inserted_rows()
    {
        InMemoryProductLearningPilotSignalRepository repo = new();

        await repo.InsertAsync(
            Signal(ProductLearningDispositionValues.Trusted, "x", "r1"),
            CancellationToken.None);

        int n = await repo.CountSignalsInScopeAsync(TenantId, WorkspaceId, ProjectId, null, CancellationToken.None);

        n.Should().Be(1);
    }

    private static ProductLearningPilotSignalRecord Signal(string disposition, string patternKey, string runId)
    {
        return new ProductLearningPilotSignalRecord
        {
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            SubjectType = ProductLearningSubjectTypeValues.RunOutput,
            Disposition = disposition,
            PatternKey = patternKey,
            ArchitectureRunId = runId,
            RecordedUtc = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc)
        };
    }
}
