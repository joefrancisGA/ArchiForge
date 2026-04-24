using ArchLucid.Contracts.Abstractions.ProductLearning;
using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Persistence.Coordination.ProductLearning;

/// <inheritdoc />
public sealed class ProductLearningDashboardService(
    IProductLearningPilotSignalRepository repository,
    IProductLearningFeedbackAggregationService aggregationService,
    IProductLearningImprovementOpportunityService opportunityService)
    : IProductLearningDashboardService
{
    public async Task<LearningDashboardSummary> GetDashboardSummaryAsync(
        ProductLearningScope scope,
        ProductLearningTriageOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(options);

        DateTime generatedUtc = DateTime.UtcNow;
        DateTime? sinceUtc = options.SinceUtc;

        Task<int> totalSignalsTask = repository.CountSignalsInScopeAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            sinceUtc,
            cancellationToken);

        Task<int> distinctRunsTask = repository.CountDistinctArchitectureRunsWithSignalsAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            sinceUtc,
            cancellationToken);

        ProductLearningAggregationSnapshot snapshot =
            await aggregationService.GetSnapshotAsync(scope, options, cancellationToken);

        IReadOnlyList<ImprovementOpportunity> opportunities =
            await opportunityService.BuildRankedOpportunitiesAsync(snapshot, options, cancellationToken);

        int totalSignals = await totalSignalsTask;
        int distinctRuns = await distinctRunsTask;

        IReadOnlyList<TriageQueueItem> triageQueue = BuildTriageQueue(opportunities, snapshot, options);

        IReadOnlyList<string> notes = BuildSummaryNotes(
            totalSignals,
            distinctRuns,
            snapshot,
            opportunities,
            options);

        return new LearningDashboardSummary
        {
            GeneratedUtc = generatedUtc,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            TotalSignalsInScope = totalSignals,
            DistinctRunsTouched = distinctRuns,
            TopAggregates = snapshot.FeedbackRollups,
            ArtifactTrends = snapshot.ArtifactTrends,
            Opportunities = opportunities,
            TriageQueue = triageQueue,
            SummaryNotes = notes
        };
    }

    private static IReadOnlyList<TriageQueueItem> BuildTriageQueue(
        IReadOnlyList<ImprovementOpportunity> opportunities,
        ProductLearningAggregationSnapshot snapshot,
        ProductLearningTriageOptions options)
    {
        List<(int Score, string TieBreaker, TriageQueueItem Item)> rows = [];

        foreach (ImprovementOpportunity opportunity in opportunities)
        {
            int score = ComputeOpportunityTriageScore(opportunity);
            string tie = "o:" + opportunity.Title;

            rows.Add((
                score,
                tie,
                new TriageQueueItem
                {
                    QueueItemId = Guid.NewGuid(),
                    RelatedSignalId = null,
                    RelatedOpportunityId = opportunity.OpportunityId,
                    Title = opportunity.Title,
                    DetailSummary = opportunity.Summary,
                    PriorityRank = 0,
                    Severity = opportunity.Severity,
                    AffectedArtifactTypeOrWorkflowArea = opportunity.AffectedArtifactTypeOrWorkflowArea,
                    TriageStatus = ProductLearningTriageStatusValues.Open,
                    FirstSeenUtc = opportunity.FirstSeenUtc,
                    LastSeenUtc = opportunity.LastSeenUtc,
                    SuggestedNextAction =
                        "Review rollup and pilot context; confirm whether to file an engineering backlog item."
                }));
        }

        foreach (RepeatedCommentTheme theme in snapshot.RepeatedCommentThemes)
        {
            if (theme.OccurrenceCount < options.MinCommentOccurrencesForTriageQueue)
                continue;


            int score = ComputeCommentThemeTriageScore(theme);
            string tie = "c:" + theme.ThemeKey;

            string severity = theme.OccurrenceCount >= 8 ? "High" : theme.OccurrenceCount >= 5 ? "Medium" : "Low";

            rows.Add((
                score,
                tie,
                new TriageQueueItem
                {
                    QueueItemId = Guid.NewGuid(),
                    RelatedSignalId = null,
                    RelatedOpportunityId = null,
                    Title = "Repeated pilot wording (" + theme.OccurrenceCount + "×)",
                    DetailSummary = "ThemeKey=" + theme.ThemeKey + "; sample=" + theme.SampleCommentShort,
                    PriorityRank = 0,
                    Severity = severity,
                    AffectedArtifactTypeOrWorkflowArea = "Feedback comments",
                    TriageStatus = ProductLearningTriageStatusValues.Open,
                    FirstSeenUtc = theme.FirstSeenUtc,
                    LastSeenUtc = theme.LastSeenUtc,
                    SuggestedNextAction =
                        "Check whether the theme maps to a documentation or UX fix; avoid over-interpreting without pilot interviews."
                }));
        }

        int maxQueue = options.MaxTriageQueueItems < 1 ? 1 : Math.Min(options.MaxTriageQueueItems, 100);

        List<TriageQueueItem> ordered = rows
            .OrderByDescending(static r => r.Score)
            .ThenBy(static r => r.TieBreaker, StringComparer.Ordinal)
            .Take(maxQueue)
            .Select(static r => r.Item)
            .ToList();

        List<TriageQueueItem> ranked = new(ordered.Count);

        for (int i = 0; i < ordered.Count; i++)

            ranked.Add(WithQueuePriority(ordered[i], i + 1));


        return ranked;
    }

    private static TriageQueueItem WithQueuePriority(TriageQueueItem item, int rank)
    {
        return new TriageQueueItem
        {
            QueueItemId = item.QueueItemId,
            RelatedSignalId = item.RelatedSignalId,
            RelatedOpportunityId = item.RelatedOpportunityId,
            Title = item.Title,
            DetailSummary = item.DetailSummary,
            PriorityRank = rank,
            Severity = item.Severity,
            AffectedArtifactTypeOrWorkflowArea = item.AffectedArtifactTypeOrWorkflowArea,
            TriageStatus = item.TriageStatus,
            FirstSeenUtc = item.FirstSeenUtc,
            LastSeenUtc = item.LastSeenUtc,
            SuggestedNextAction = item.SuggestedNextAction
        };
    }

    private static int ComputeOpportunityTriageScore(ImprovementOpportunity opportunity)
    {
        int band = opportunity.Severity == "High" ? 1_000_000 : opportunity.Severity == "Medium" ? 600_000 : 200_000;
        int rankBoost = 10_000 - Math.Clamp(opportunity.PriorityRank, 0, 9_999);

        return band + rankBoost;
    }

    private static int ComputeCommentThemeTriageScore(RepeatedCommentTheme theme)
    {
        return 400_000 + Math.Min(theme.OccurrenceCount, 9_999) * 100;
    }

    private static IReadOnlyList<string> BuildSummaryNotes(
        int totalSignals,
        int distinctRuns,
        ProductLearningAggregationSnapshot snapshot,
        IReadOnlyList<ImprovementOpportunity> opportunities,
        ProductLearningTriageOptions options)
    {
        List<string> lines =
        [
            "Total signals in scope: " + totalSignals + "; distinct architecture runs with signals: " + distinctRuns +
            ".",
            "Rollups shown require at least " + options.MinSignalsPerAggregate + " signal(s) (noise gate).",
            "Artifact trends require at least " + options.MinNegativeOutcomesOnArtifactTrend +
            " negative outcomes (reject/revise/follow-up) and sufficient total signals.",
            "Improvement opportunities ranked by weighted bad-score (rejects > follow-ups > revisions) with a small bonus when no trusted signals exist on a multi-signal rollup.",
            "Repeated comments use the first " + ProductLearningSignalAggregations.CommentThemePrefixLength +
            " characters after trim (deterministic, not semantic)."
        ];

        return lines;
    }
}
