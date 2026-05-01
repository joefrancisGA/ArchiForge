using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;

namespace ArchLucid.Persistence.Tests.ProductLearning.Planning;

[Trait("ChangeSet", "59R")]
public sealed class LearningPlanningReportBuilderTests
{
    private static ProductLearningScope Scope()
    {
        return new ProductLearningScope
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };
    }

    [SkippableFact]
    public async Task BuildAsync_orders_themes_by_evidence_then_runs_then_id()
    {
        InMemoryProductLearningPlanningRepository repo = new();
        ProductLearningScope scope = Scope();

        await repo.InsertThemeAsync(
            Theme(scope, 1, 5, "low"),
            CancellationToken.None);

        await repo.InsertThemeAsync(
            Theme(scope, 10, 1, "high"),
            CancellationToken.None);

        LearningPlanningReportLimits limits = new()
        {
            MaxThemes = 50,
            MaxPlans = 50,
            MaxSignalRefsPerPlan = 100,
            MaxArtifactRefsPerPlan = 100,
            MaxRunRefsPerPlan = 100
        };

        LearningPlanningReportDocument doc =
            await LearningPlanningReportBuilder.BuildAsync(repo, scope, limits, CancellationToken.None);

        Assert.Equal(2, doc.Themes.Count);
        Assert.Equal("high", doc.Themes[0].ThemeKey);
        Assert.Equal("low", doc.Themes[1].ThemeKey);
    }

    [SkippableFact]
    public async Task BuildAsync_orders_plans_by_priority_then_plan_id_and_includes_evidence_refs()
    {
        InMemoryProductLearningPlanningRepository repo = new();
        ProductLearningScope scope = Scope();

        await repo.InsertThemeAsync(
            Theme(scope, 3, 1, "th"),
            CancellationToken.None);

        Guid themeId = (await repo.ListThemesAsync(scope, 1, CancellationToken.None))[0].ThemeId;

        await repo.InsertPlanAsync(
            Plan(scope, themeId, "LowPri", 1, "b"),
            CancellationToken.None);

        await repo.InsertPlanAsync(
            Plan(scope, themeId, "HighPri", 99, "a"),
            CancellationToken.None);

        IReadOnlyList<ProductLearningImprovementPlanRecord> listed =
            await repo.ListPlansAsync(scope, 10, CancellationToken.None);
        Guid lowId = listed.Single(p => p.Title == "LowPri").PlanId;
        Guid highId = listed.Single(p => p.Title == "HighPri").PlanId;

        Guid sig = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await repo.AddPlanSignalLinkAsync(
            new ProductLearningImprovementPlanSignalLinkRecord
            {
                PlanId = lowId,
                SignalId = sig,
                TriageStatusSnapshot = ProductLearningTriageStatusValues.Open
            },
            CancellationToken.None);

        await repo.AddPlanArchitectureRunLinkAsync(
            new ProductLearningImprovementPlanRunLinkRecord { PlanId = highId, ArchitectureRunId = "run-z" },
            CancellationToken.None);

        LearningPlanningReportLimits limits = new()
        {
            MaxThemes = 50,
            MaxPlans = 50,
            MaxSignalRefsPerPlan = 100,
            MaxArtifactRefsPerPlan = 100,
            MaxRunRefsPerPlan = 100
        };

        LearningPlanningReportDocument doc =
            await LearningPlanningReportBuilder.BuildAsync(repo, scope, limits, CancellationToken.None);

        Assert.Equal(2, doc.Plans.Count);
        Assert.Equal("HighPri", doc.Plans[0].Title);
        Assert.Equal("LowPri", doc.Plans[1].Title);
        Assert.Empty(doc.Plans[0].Evidence.Signals);
        Assert.Single(doc.Plans[1].Evidence.Signals);
        Assert.Equal(sig, doc.Plans[1].Evidence.Signals[0].SignalId);
        Assert.Equal(1, doc.Summary.TotalLinkedSignalsAcrossPlans);
        Assert.Single(doc.Plans[0].Evidence.ArchitectureRunIds);
        Assert.Equal("run-z", doc.Plans[0].Evidence.ArchitectureRunIds[0]);
    }

    [SkippableFact]
    public async Task BuildAsync_caps_evidence_ref_lists()
    {
        InMemoryProductLearningPlanningRepository repo = new();
        ProductLearningScope scope = Scope();

        await repo.InsertThemeAsync(Theme(scope, 1, 1, "k"), CancellationToken.None);
        Guid themeId = (await repo.ListThemesAsync(scope, 1, CancellationToken.None))[0].ThemeId;
        await repo.InsertPlanAsync(Plan(scope, themeId, "P", 1, "x"), CancellationToken.None);
        Guid planId = (await repo.ListPlansAsync(scope, 1, CancellationToken.None))[0].PlanId;

        Guid[] signalIds =
        [
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")
        ];

        foreach (Guid sid in signalIds)
        {
            await repo.AddPlanSignalLinkAsync(
                new ProductLearningImprovementPlanSignalLinkRecord { PlanId = planId, SignalId = sid },
                CancellationToken.None);
        }

        LearningPlanningReportLimits limits = new()
        {
            MaxThemes = 50,
            MaxPlans = 50,
            MaxSignalRefsPerPlan = 2,
            MaxArtifactRefsPerPlan = 100,
            MaxRunRefsPerPlan = 100
        };

        LearningPlanningReportDocument doc =
            await LearningPlanningReportBuilder.BuildAsync(repo, scope, limits, CancellationToken.None);

        Assert.Equal(5, doc.Plans[0].Evidence.LinkedSignalCount);
        Assert.Equal(2, doc.Plans[0].Evidence.Signals.Count);
    }

    private static ProductLearningImprovementThemeRecord Theme(
        ProductLearningScope scope,
        int evidence,
        int runs,
        string key)
    {
        return new ProductLearningImprovementThemeRecord
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ThemeKey = key,
            Title = key,
            Summary = "s",
            AffectedArtifactTypeOrWorkflowArea = "a",
            SeverityBand = "Low",
            EvidenceSignalCount = evidence,
            DistinctRunCount = runs,
            DerivationRuleVersion = "59R-v1"
        };
    }

    private static ProductLearningImprovementPlanRecord Plan(
        ProductLearningScope scope,
        Guid themeId,
        string title,
        int priority,
        string planKey)
    {
        return new ProductLearningImprovementPlanRecord
        {
            ThemeId = themeId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            Title = title,
            Summary = planKey,
            PriorityScore = priority,
            ActionSteps =
            [
                new ProductLearningImprovementPlanActionStep { Ordinal = 1, ActionType = "A", Description = "d" }
            ]
        };
    }
}
