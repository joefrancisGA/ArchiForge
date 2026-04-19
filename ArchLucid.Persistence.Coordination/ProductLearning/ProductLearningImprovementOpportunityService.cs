using ArchLucid.Contracts.Abstractions.ProductLearning;
using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Persistence.Coordination.ProductLearning;

/// <inheritdoc />
public sealed class ProductLearningImprovementOpportunityService : IProductLearningImprovementOpportunityService
{
    public Task<IReadOnlyList<ImprovementOpportunity>> BuildRankedOpportunitiesAsync(
        ProductLearningAggregationSnapshot snapshot,
        ProductLearningTriageOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);

        HashSet<string> usedKeys = new(StringComparer.Ordinal);
        List<(int BadScore, string SortKey, ImprovementOpportunity Model)> work = [];

        foreach (FeedbackAggregate aggregate in snapshot.FeedbackRollups)
        {
            if (aggregate.TotalSignalCount < options.MinSignalsPerAggregate)

                continue;


            int badScore = ProductLearningOpportunityScoring.ComputeAggregateBadScore(aggregate);
            bool passesThreshold =
                badScore >= options.MinAggregateBadScoreForOpportunity ||
                aggregate.RejectedCount + aggregate.NeedsFollowUpCount >= 2 ||
                aggregate.RevisedCount >= 2;

            if (!passesThreshold)

                continue;


            if (!usedKeys.Add(aggregate.AggregateKey))

                continue;


            string sortKey = "a:" + aggregate.AggregateKey;
            work.Add((badScore, sortKey, ProductLearningOpportunityScoring.MapAggregateToOpportunity(aggregate, badScore, priorityRank: 0)));
        }

        foreach (ArtifactOutcomeTrend trend in snapshot.ArtifactTrends)
        {
            if (ProductLearningOpportunityScoring.TotalTrendSignals(trend) < options.MinSignalsPerAggregate)

                continue;


            int negative = ProductLearningOpportunityScoring.ComputeTrendNegativeMass(trend);

            if (negative < options.MinNegativeOutcomesOnArtifactTrend)

                continue;


            string dedupeKey = "trend:" + trend.TrendKey;

            if (!usedKeys.Add(dedupeKey))

                continue;


            int badScore = negative * 3 + trend.RejectionCount;
            string sortKey = "t:" + trend.TrendKey;
            work.Add((badScore, sortKey, ProductLearningOpportunityScoring.MapTrendToOpportunity(trend, badScore, priorityRank: 0)));
        }

        int max = options.MaxImprovementOpportunities < 1 ? 1 : Math.Min(options.MaxImprovementOpportunities, 100);

        List<ImprovementOpportunity> ordered = work
            .OrderByDescending(static x => x.BadScore)
            .ThenByDescending(static x => x.Model.LastSeenUtc)
            .ThenBy(static x => x.SortKey, StringComparer.Ordinal)
            .Take(max)
            .Select(static x => x.Model)
            .ToList();

        List<ImprovementOpportunity> ranked = new(ordered.Count);

        for (int i = 0; i < ordered.Count; i++)

            ranked.Add(WithPriorityRank(ordered[i], i + 1));


        return Task.FromResult<IReadOnlyList<ImprovementOpportunity>>(ranked);
    }

    private static ImprovementOpportunity WithPriorityRank(ImprovementOpportunity model, int rank) =>
        new()
        {
            OpportunityId = model.OpportunityId,
            SourceAggregateKey = model.SourceAggregateKey,
            PatternKey = model.PatternKey,
            Title = model.Title,
            Summary = model.Summary,
            AffectedArtifactTypeOrWorkflowArea = model.AffectedArtifactTypeOrWorkflowArea,
            Severity = model.Severity,
            PriorityRank = rank,
            SuggestedOwnerRole = model.SuggestedOwnerRole,
            EvidenceSignalCount = model.EvidenceSignalCount,
            DistinctRunCount = model.DistinctRunCount,
            AverageTrustScore = model.AverageTrustScore,
            RepeatedThemeSnippet = model.RepeatedThemeSnippet,
            FirstSeenUtc = model.FirstSeenUtc,
            LastSeenUtc = model.LastSeenUtc,
        };
}
