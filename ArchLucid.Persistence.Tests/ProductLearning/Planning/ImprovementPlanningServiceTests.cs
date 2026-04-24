using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;

namespace ArchLucid.Persistence.Tests.ProductLearning.Planning;

/// <summary>59R improvement plan generation: deterministic ids, templates, and priority scoring.</summary>
[Trait("ChangeSet", "59R")]
public sealed class ImprovementPlanningServiceTests
{
    private static ImprovementThemeWithEvidence ThemeWithKey(
        Guid themeId,
        string canonicalKey,
        string name,
        int evidenceCount,
        string[] facets)
    {
        return new ImprovementThemeWithEvidence
        {
            CanonicalKey = canonicalKey,
            GroupingExplanation = "test grouping",
            Theme = new ImprovementTheme
            {
                ThemeId = themeId,
                Name = name,
                Description = "d",
                EvidenceCount = evidenceCount,
                AffectedArtifactTypes = facets,
                FirstSeenUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                LastSeenUtc = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            ExampleEvidence =
            [
                new ImprovementThemeEvidence
                {
                    EvidenceId = Guid.NewGuid(),
                    ThemeId = themeId,
                    ArchitectureRunId = "run-1",
                    SignalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                }
            ]
        };
    }

    private static ImprovementThemeWithEvidence TrendTheme(string name, string facet)
    {
        Guid themeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        return new ImprovementThemeWithEvidence
        {
            CanonicalKey = "trend:RunOutput|" + facet,
            GroupingExplanation = "Artifact trend grouping (test).",
            Theme = new ImprovementTheme
            {
                ThemeId = themeId,
                Name = name,
                Description = "reject=2 revised=1 followUp=0 trusted=1 total=4",
                EvidenceCount = 4,
                AffectedArtifactTypes = [facet],
                FirstSeenUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                LastSeenUtc = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            ExampleEvidence =
            [
                new ImprovementThemeEvidence
                {
                    EvidenceId = Guid.NewGuid(),
                    ThemeId = themeId,
                    ArchitectureRunId = "run-1",
                    SignalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                }
            ]
        };
    }

    [Fact]
    public async Task Trend_plan_has_five_steps_and_stable_id()
    {
        ImprovementPlanningService svc = new();

        ImprovementThemeWithEvidence theme = TrendTheme(
            "Improve architecture summary readability",
            "architecture-summary.md");

        ImprovementPlanningOptions options = new()
        {
            RuleVersion = "59R-plan-v1", CreatedUtcOverride = new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc)
        };

        IReadOnlyList<ImprovementPlan> plans = await svc.BuildPlansAsync(
            [theme],
            options,
            CancellationToken.None);

        ImprovementPlan plan = Assert.Single(plans);

        Assert.Equal(theme.Theme.ThemeId, plan.ThemeId);
        Assert.Equal(5, plan.ProposedChanges.Count);
        Assert.Equal("Content", plan.ProposedChanges[1].ActionType);
        Assert.Contains("architecture-summary.md", plan.Description, StringComparison.Ordinal);
        Assert.True(plan.PriorityScore > 0);

        IReadOnlyList<ImprovementPlan> again = await svc.BuildPlansAsync(
            [theme],
            options,
            CancellationToken.None);

        Assert.Equal(plan.PlanId, again[0].PlanId);
    }

    [Fact]
    public async Task Max_steps_trims_template()
    {
        ImprovementPlanningService svc = new();

        ImprovementThemeWithEvidence theme = TrendTheme("T", "diagram");

        IReadOnlyList<ImprovementPlan> plans = await svc.BuildPlansAsync(
            [theme],
            new ImprovementPlanningOptions { MaxStepsPerPlan = 3, RuleVersion = "v1" },
            CancellationToken.None);

        Assert.Equal(3, plans[0].ProposedChanges.Count);
        Assert.Equal(1, plans[0].ProposedChanges[0].Ordinal);
        Assert.Equal(3, plans[0].ProposedChanges[2].Ordinal);
    }

    [Fact]
    public async Task Priority_scores_severity_by_canonical_prefix_and_frequency_from_evidence_count()
    {
        ImprovementPlanningService svc = new();
        Guid idRollup = Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid idUnknown = Guid.Parse("20000000-0000-0000-0000-000000000002");

        ImprovementThemeWithEvidence rollup = ThemeWithKey(
            idRollup,
            "rollup:pat-a",
            "R",
            2,
            ["a"]);

        ImprovementThemeWithEvidence unknown = ThemeWithKey(
            idUnknown,
            "custom:opaque",
            "U",
            2,
            ["b"]);

        ImprovementPlanningOptions options = new() { RuleVersion = "v1", MaxStepsPerPlan = 1 };

        IReadOnlyList<ImprovementPlan> plans = await svc.BuildPlansAsync(
            [unknown, rollup],
            options,
            CancellationToken.None);

        ImprovementPlan pRollup = plans.Single(p => p.ThemeId == idRollup);
        ImprovementPlan pUnknown = plans.Single(p => p.ThemeId == idUnknown);

        Assert.Equal(320, pRollup.SeverityScore);
        Assert.Equal(160, pUnknown.SeverityScore);
        Assert.Equal(60, pRollup.FrequencyScore);
        Assert.Equal(60, pUnknown.FrequencyScore);
        Assert.True(pRollup.PriorityScore > pUnknown.PriorityScore);
        Assert.Equal(pRollup.FrequencyScore + pRollup.SeverityScore, pRollup.PriorityScore);
    }

    [Fact]
    public async Task Frequency_score_respects_upper_cap()
    {
        ImprovementPlanningService svc = new();
        Guid themeId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        ImprovementThemeWithEvidence heavy = ThemeWithKey(
            themeId,
            "rollup:x",
            "H",
            50,
            ["x"]);

        IReadOnlyList<ImprovementPlan> plans = await svc.BuildPlansAsync(
            [heavy],
            new ImprovementPlanningOptions { RuleVersion = "v1", MaxStepsPerPlan = 1 },
            CancellationToken.None);

        ImprovementPlan plan = Assert.Single(plans);

        Assert.Equal(600, plan.FrequencyScore);
        Assert.Equal(920, plan.PriorityScore);
    }

    [Fact]
    public async Task Output_order_is_theme_id_then_canonical_key_regardless_of_input_order()
    {
        ImprovementPlanningService svc = new();
        Guid lowId = Guid.Parse("01000000-0000-0000-0000-000000000001");
        Guid highId = Guid.Parse("02000000-0000-0000-0000-000000000002");

        ImprovementThemeWithEvidence second = ThemeWithKey(
            highId,
            "trend:RunOutput|z",
            "Z",
            1,
            ["z"]);

        ImprovementThemeWithEvidence first = ThemeWithKey(
            lowId,
            "trend:RunOutput|a",
            "A",
            1,
            ["a"]);

        IReadOnlyList<ImprovementPlan> plans = await svc.BuildPlansAsync(
            [second, first],
            new ImprovementPlanningOptions { RuleVersion = "v1", MaxStepsPerPlan = 1 },
            CancellationToken.None);

        Assert.Equal(2, plans.Count);
        Assert.Equal(lowId, plans[0].ThemeId);
        Assert.Equal(highId, plans[1].ThemeId);
    }

    [Fact]
    public async Task Rollup_template_starts_with_investigate_comment_template_has_four_steps()
    {
        ImprovementPlanningService svc = new();
        Guid rollupId = Guid.Parse("40000000-0000-0000-0000-000000000004");
        Guid commentId = Guid.Parse("50000000-0000-0000-0000-000000000005");

        ImprovementThemeWithEvidence rollup = ThemeWithKey(
            rollupId,
            "rollup:key1",
            "Roll",
            1,
            ["f"]);

        ImprovementThemeWithEvidence comment = ThemeWithKey(
            commentId,
            "comment:hello",
            "C",
            1,
            ["Feedback comments"]);

        IReadOnlyList<ImprovementPlan> plans = await svc.BuildPlansAsync(
            [rollup, comment],
            new ImprovementPlanningOptions { RuleVersion = "v1", MaxStepsPerPlan = 10 },
            CancellationToken.None);

        ImprovementPlan pRoll = plans.Single(p => p.ThemeId == rollupId);
        ImprovementPlan pCom = plans.Single(p => p.ThemeId == commentId);

        Assert.Equal("Investigate", pRoll.ProposedChanges[0].ActionType);
        Assert.Equal(5, pRoll.ProposedChanges.Count);

        Assert.Equal(4, pCom.ProposedChanges.Count);
        Assert.Equal(["Investigate", "UX", "Content", "Verify"],
            pCom.ProposedChanges.Select(s => s.ActionType).ToArray());
    }
}
