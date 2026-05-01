using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.ProductLearning.Planning;

namespace ArchLucid.Persistence.Tests.Evolution;

[Trait("Category", "Unit")]
public sealed class CandidateChangeSetServiceTests
{
    private readonly CandidateChangeSetService _sut = new();

    [SkippableFact]
    public void MapFromImprovementPlan_NullPlan_Throws()
    {
        Action act = () => _sut.MapFromImprovementPlan(null!, null);

        act.Should().Throw<ArgumentNullException>().WithParameterName("plan");
    }

    [SkippableFact]
    public void MapFromImprovementPlan_SingleStep_ReturnsOneAggregateOnly_WithDeterministicId()
    {
        Guid planId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DateTime created = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        ProductLearningImprovementPlanRecord plan = new()
        {
            PlanId = planId,
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ThemeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Title = "T",
            Summary = "S",
            ActionSteps =
            [
                new ProductLearningImprovementPlanActionStep { Ordinal = 1, ActionType = "A", Description = "only" }
            ],
            PriorityScore = 5,
            PriorityExplanation = "Because",
            CreatedUtc = created
        };

        IReadOnlyList<CandidateChangeSet> first = _sut.MapFromImprovementPlan(plan, null);
        IReadOnlyList<CandidateChangeSet> second = _sut.MapFromImprovementPlan(plan, null);

        first.Should().HaveCount(1);
        first.Zip(second, static (a, b) => ChangeSetsEqual(a, b)).Should().OnlyContain(static x => x);

        CandidateChangeSet aggregate = first[0];
        aggregate.ChangeSetId.Should().Be(CandidateChangeSetDeterministicIds.AggregateChangeSetId(planId));
        aggregate.SourcePlanId.Should().Be(planId);
        aggregate.ProposedActions.Should().HaveCount(1);
        aggregate.ProposedActions[0].Ordinal.Should().Be(1);
        aggregate.ProposedActions[0].Description.Should().Be("only");
        aggregate.ApprovalStatus.Should().Be(ApprovalStatus.PendingReview);
        aggregate.CreatedUtc.Should().Be(created);
        aggregate.SimulationScore.Should().BeNull();
        aggregate.ExpectedImpact.Summary.Should().Be("Because");
        aggregate.AffectedComponents.Should().ContainSingle()
            .Which.ComponentKey.Should().Be(planId.ToString("N"));
    }

    [SkippableFact]
    public void MapFromImprovementPlan_TwoSteps_ReturnsAggregatePlusTwoSlices_OrderedByOrdinal()
    {
        Guid planId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Guid themeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        ProductLearningImprovementThemeRecord theme = new()
        {
            ThemeId = themeId,
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ThemeKey = "key-x",
            Title = "Theme title",
            Summary = "Theme sum",
            AffectedArtifactTypeOrWorkflowArea = "API",
            SeverityBand = "High",
            EvidenceSignalCount = 3,
            DistinctRunCount = 2,
            DerivationRuleVersion = "59R-test",
            PatternKey = "pattern-p"
        };

        ProductLearningImprovementPlanRecord plan = new()
        {
            PlanId = planId,
            TenantId = theme.TenantId,
            WorkspaceId = theme.WorkspaceId,
            ProjectId = theme.ProjectId,
            ThemeId = themeId,
            Title = "Plan title",
            Summary = "Plan summary",
            ActionSteps =
            [
                new ProductLearningImprovementPlanActionStep { Ordinal = 2, ActionType = "X", Description = "d2" },
                new ProductLearningImprovementPlanActionStep { Ordinal = 1, ActionType = "Y", Description = "d1" }
            ],
            PriorityScore = 9,
            CreatedUtc = DateTime.UtcNow
        };

        IReadOnlyList<CandidateChangeSet> results = _sut.MapFromImprovementPlan(plan, theme);

        results.Should().HaveCount(3);

        results[0].ProposedActions.Should().HaveCount(2);
        results[1].ProposedActions.Should().HaveCount(1);
        results[1].ProposedActions[0].Ordinal.Should().Be(1);
        results[2].ProposedActions.Should().HaveCount(1);
        results[2].ProposedActions[0].Ordinal.Should().Be(2);

        results[0].ChangeSetId.Should().Be(CandidateChangeSetDeterministicIds.AggregateChangeSetId(planId));
        results[1].ChangeSetId.Should().Be(CandidateChangeSetDeterministicIds.StepSliceChangeSetId(planId, 1));
        results[2].ChangeSetId.Should().Be(CandidateChangeSetDeterministicIds.StepSliceChangeSetId(planId, 2));

        results[0].AffectedComponents.Should().HaveCount(2);
        results[0].AffectedComponents[0].ComponentKey.Should().Be("key-x");
        results[0].AffectedComponents[1].ComponentKey.Should().Be("pattern-p");
    }

    [SkippableFact]
    public void MapFromImprovementPlan_ShuffledInputSteps_ProducesSameOutputAsSortedInput()
    {
        Guid planId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        ProductLearningImprovementPlanRecord shuffled = new()
        {
            PlanId = planId,
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ThemeId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            Title = "T",
            Summary = "S",
            ActionSteps =
            [
                new ProductLearningImprovementPlanActionStep { Ordinal = 2, ActionType = "A", Description = "b" },
                new ProductLearningImprovementPlanActionStep { Ordinal = 1, ActionType = "A", Description = "a" }
            ],
            PriorityScore = 1,
            CreatedUtc = DateTime.UtcNow
        };

        ProductLearningImprovementPlanRecord sorted = new()
        {
            PlanId = planId,
            TenantId = shuffled.TenantId,
            WorkspaceId = shuffled.WorkspaceId,
            ProjectId = shuffled.ProjectId,
            ThemeId = shuffled.ThemeId,
            Title = shuffled.Title,
            Summary = shuffled.Summary,
            ActionSteps =
            [
                shuffled.ActionSteps[1],
                shuffled.ActionSteps[0]
            ],
            PriorityScore = shuffled.PriorityScore,
            CreatedUtc = shuffled.CreatedUtc
        };

        IReadOnlyList<CandidateChangeSet> a = _sut.MapFromImprovementPlan(shuffled, null);
        IReadOnlyList<CandidateChangeSet> b = _sut.MapFromImprovementPlan(sorted, null);

        a.Zip(b, static (x, y) => ChangeSetsEqual(x, y)).Should().OnlyContain(static x => x);
    }

    private static bool ChangeSetsEqual(CandidateChangeSet? left, CandidateChangeSet? right)
    {
        if (left is null || right is null)
        {
            return left == right;
        }

        if (left.ChangeSetId != right.ChangeSetId || left.SourcePlanId != right.SourcePlanId)
        {
            return false;
        }

        if (left.Description != right.Description || left.ApprovalStatus != right.ApprovalStatus)
        {
            return false;
        }

        if (left.CreatedUtc != right.CreatedUtc)
        {
            return false;
        }

        if (left.ExpectedImpact.Summary != right.ExpectedImpact.Summary ||
            left.ExpectedImpact.Rationale != right.ExpectedImpact.Rationale)
        {
            return false;
        }

        if (left.ProposedActions.Count != right.ProposedActions.Count)
        {
            return false;
        }

        for (int i = 0; i < left.ProposedActions.Count; i++)
        {
            CandidateChangeSetStep s = left.ProposedActions[i];
            CandidateChangeSetStep t = right.ProposedActions[i];

            if (s.Ordinal != t.Ordinal || s.ActionType != t.ActionType || s.Description != t.Description ||
                s.AcceptanceCriteria != t.AcceptanceCriteria)
            {
                return false;
            }
        }

        if (left.AffectedComponents.Count != right.AffectedComponents.Count)
        {
            return false;
        }

        for (int i = 0; i < left.AffectedComponents.Count; i++)
        {
            ChangeSetAffectedComponent c = left.AffectedComponents[i];
            ChangeSetAffectedComponent d = right.AffectedComponents[i];

            if (c.ComponentKey != d.ComponentKey || c.DisplayName != d.DisplayName || c.WorkflowArea != d.WorkflowArea)
            {
                return false;
            }
        }

        return true;
    }
}
