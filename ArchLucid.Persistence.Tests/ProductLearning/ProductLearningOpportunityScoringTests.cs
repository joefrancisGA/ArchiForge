using ArchLucid.Contracts.ProductLearning;

namespace ArchLucid.Persistence.Tests.ProductLearning;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
[Trait("ChangeSet", "58R")]
public sealed class ProductLearningOpportunityScoringTests
{
    [SkippableFact]
    public void ComputeAggregateBadScore_weights_reject_followup_revise_and_no_trusted_bonus()
    {
        FeedbackAggregate agg = new()
        {
            AggregateKey = "k",
            SubjectTypeOrWorkflowArea = "RunOutput",
            TotalSignalCount = 3,
            TrustedCount = 0,
            RejectedCount = 1,
            NeedsFollowUpCount = 1,
            RevisedCount = 1,
            FirstSignalRecordedUtc = DateTime.UtcNow,
            LastSignalRecordedUtc = DateTime.UtcNow
        };

        int score = ProductLearningOpportunityScoring.ComputeAggregateBadScore(agg);

        // 1*4 + 1*3 + 1*2 + 2 (no-trusted multi-signal bonus) = 11
        score.Should().Be(11);
    }

    [SkippableFact]
    public void SeverityFromBadScore_maps_to_high_medium_low_bands()
    {
        ProductLearningOpportunityScoring.SeverityFromBadScore(11).Should().Be("Medium");
        ProductLearningOpportunityScoring.SeverityFromBadScore(12).Should().Be("High");
        ProductLearningOpportunityScoring.SeverityFromBadScore(5).Should().Be("Low");
    }

    [SkippableFact]
    public void ComputeTrendNegativeMass_sums_reject_revise_followup()
    {
        ArtifactOutcomeTrend trend = new()
        {
            TrendKey = "t",
            ArtifactTypeOrHint = "x",
            AcceptedOrTrustedCount = 5,
            RejectionCount = 1,
            RevisionCount = 2,
            NeedsFollowUpCount = 1,
            DistinctRunCount = 2,
            FirstSeenUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow
        };

        ProductLearningOpportunityScoring.ComputeTrendNegativeMass(trend).Should().Be(4);
        ProductLearningOpportunityScoring.TotalTrendSignals(trend).Should().Be(9);
    }
}
