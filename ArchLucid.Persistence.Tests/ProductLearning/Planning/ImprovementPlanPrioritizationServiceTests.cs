using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;

namespace ArchLucid.Persistence.Tests.ProductLearning.Planning;

public sealed class ImprovementPlanPrioritizationServiceTests
{
    private static ImprovementPlan Plan(Guid planId, int seedPriority) =>
        new()
        {
            PlanId = planId,
            ThemeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Title = "T",
            Description = "D",
            ProposedChanges = [],
            PriorityScore = seedPriority,
            FrequencyScore = 1,
            SeverityScore = 2,
            TrustImpactScore = 0,
            CreatedUtc = DateTime.UtcNow,
        };

    [Fact]
    public async Task Higher_frequency_and_severity_ranks_first_with_default_weights()
    {
        ImprovementPlanPrioritizationService svc = new();

        ImprovementPlanScoreInput low = new()
        {
            Plan = Plan(Guid.Parse("11111111-1111-1111-1111-111111111111"), 0),
            EvidenceSignalCount = 2,
            RejectedCount = 0,
            RevisedCount = 0,
            NeedsFollowUpCount = 0,
            AverageTrustScore = 0.9,
            AffectedArtifactTypeCount = 1,
        };

        ImprovementPlanScoreInput high = new()
        {
            Plan = Plan(Guid.Parse("22222222-2222-2222-2222-222222222222"), 0),
            EvidenceSignalCount = 20,
            RejectedCount = 5,
            RevisedCount = 2,
            NeedsFollowUpCount = 1,
            AverageTrustScore = 0.2,
            AffectedArtifactTypeCount = 4,
        };

        IReadOnlyList<ImprovementPlan> ranked = await svc.RankPlansAsync(
            [low, high],
            new ImprovementPlanPrioritizationWeights(),
            CancellationToken.None);

        Assert.Equal(high.Plan.PlanId, ranked[0].PlanId);
        Assert.True(ranked[0].PriorityScore > ranked[1].PriorityScore);
        Assert.NotNull(ranked[0].PrioritizationExplanation);
        Assert.Contains("weights frequency=0.4", ranked[0].PrioritizationExplanation, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Weight_sum_must_be_one()
    {
        ImprovementPlanPrioritizationService svc = new();

        await Assert.ThrowsAsync<ArgumentException>(() => svc.RankPlansAsync(
            [
                new ImprovementPlanScoreInput
                {
                    Plan = Plan(Guid.NewGuid(), 0),
                    EvidenceSignalCount = 1,
                }
            ],
            new ImprovementPlanPrioritizationWeights
            {
                Frequency = 0.5,
                Severity = 0.5,
                TrustImpact = 0.5,
                Breadth = 0.5,
            },
            CancellationToken.None));
    }

    [Fact]
    public async Task Tie_breaker_uses_plan_id_ascending()
    {
        ImprovementPlanPrioritizationService svc = new();

        ImprovementPlanScoreInput a = new()
        {
            Plan = Plan(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 0),
            EvidenceSignalCount = 5,
            RejectedCount = 1,
            RevisedCount = 0,
            NeedsFollowUpCount = 0,
            AverageTrustScore = null,
            AffectedArtifactTypeCount = 1,
        };

        ImprovementPlanScoreInput b = new()
        {
            Plan = Plan(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 0),
            EvidenceSignalCount = 5,
            RejectedCount = 1,
            RevisedCount = 0,
            NeedsFollowUpCount = 0,
            AverageTrustScore = null,
            AffectedArtifactTypeCount = 1,
        };

        IReadOnlyList<ImprovementPlan> ranked = await svc.RankPlansAsync(
            [a, b],
            new ImprovementPlanPrioritizationWeights(),
            CancellationToken.None);

        Assert.Equal(b.Plan.PlanId, ranked[0].PlanId);
        Assert.Equal(a.Plan.PlanId, ranked[1].PlanId);
    }
}
