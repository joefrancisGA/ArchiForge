using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Contracts.ProductLearning.Planning;
using ArchiForge.Persistence.ProductLearning.Planning;

namespace ArchiForge.Persistence.Tests.ProductLearning.Planning;

public sealed class ProductLearningPlanningRepositoryTests
{
    private static ProductLearningScope Scope() =>
        new()
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };

    [Fact]
    public async Task InsertTheme_list_and_get_round_trips()
    {
        InMemoryProductLearningPlanningRepository repo = new();
        ProductLearningScope scope = Scope();
        DateTime utc = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);

        ProductLearningImprovementThemeRecord theme = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ThemeKey = "agg:pattern-x|59R-v1",
            SourceAggregateKey = "pattern-x",
            PatternKey = "pattern-x",
            Title = "Theme title",
            Summary = "Summary body",
            AffectedArtifactTypeOrWorkflowArea = "RunOutput",
            SeverityBand = "High",
            EvidenceSignalCount = 5,
            DistinctRunCount = 2,
            AverageTrustScore = 0.4,
            DerivationRuleVersion = "59R-v1",
            CreatedUtc = utc
        };

        await repo.InsertThemeAsync(theme, CancellationToken.None);

        IReadOnlyList<ProductLearningImprovementThemeRecord> listed =
            await repo.ListThemesAsync(scope, 10, CancellationToken.None);

        Assert.Single(listed);
        Assert.Equal(theme.ThemeKey, listed[0].ThemeKey);

        ProductLearningImprovementThemeRecord? got =
            await repo.GetThemeAsync(listed[0].ThemeId, scope, CancellationToken.None);

        Assert.NotNull(got);
        Assert.Equal("Theme title", got.Title);
        Assert.Equal(5, got.EvidenceSignalCount);
    }

    [Fact]
    public async Task Duplicate_theme_key_in_scope_throws()
    {
        InMemoryProductLearningPlanningRepository repo = new();
        ProductLearningScope scope = Scope();

        ProductLearningImprovementThemeRecord theme = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ThemeKey = "dup-key",
            Title = "A",
            Summary = "S",
            AffectedArtifactTypeOrWorkflowArea = "X",
            SeverityBand = "Low",
            EvidenceSignalCount = 1,
            DistinctRunCount = 1,
            DerivationRuleVersion = "59R-v1"
        };

        await repo.InsertThemeAsync(theme, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.InsertThemeAsync(theme, CancellationToken.None));
    }

    [Fact]
    public async Task Insert_plan_requires_theme_and_round_trips_action_steps()
    {
        InMemoryProductLearningPlanningRepository repo = new();
        ProductLearningScope scope = Scope();

        ProductLearningImprovementThemeRecord theme = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ThemeKey = "t1",
            Title = "T",
            Summary = "S",
            AffectedArtifactTypeOrWorkflowArea = "A",
            SeverityBand = "Medium",
            EvidenceSignalCount = 3,
            DistinctRunCount = 1,
            DerivationRuleVersion = "59R-v1"
        };

        await repo.InsertThemeAsync(theme, CancellationToken.None);

        ProductLearningImprovementThemeRecord stored =
            (await repo.ListThemesAsync(scope, 1, CancellationToken.None))[0];

        ProductLearningImprovementPlanRecord orphan = new()
        {
            ThemeId = Guid.NewGuid(),
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            Title = "Plan",
            Summary = "P",
            ActionSteps =
            [
                new ProductLearningImprovementPlanActionStep
                {
                    Ordinal = 1,
                    ActionType = "Investigate",
                    Description = "Read signals"
                }
            ],
            PriorityScore = 42
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.InsertPlanAsync(orphan, CancellationToken.None));

        ProductLearningImprovementPlanRecord plan = new()
        {
            ThemeId = stored.ThemeId,
            TenantId = orphan.TenantId,
            WorkspaceId = orphan.WorkspaceId,
            ProjectId = orphan.ProjectId,
            Title = orphan.Title,
            Summary = orphan.Summary,
            ActionSteps = orphan.ActionSteps,
            PriorityScore = orphan.PriorityScore
        };

        await repo.InsertPlanAsync(plan, CancellationToken.None);

        ProductLearningImprovementPlanRecord? got =
            await repo.GetPlanAsync(
                (await repo.ListPlansAsync(scope, 5, CancellationToken.None))[0].PlanId,
                scope,
                CancellationToken.None);

        Assert.NotNull(got);
        Assert.Single(got.ActionSteps);
        Assert.Equal("Investigate", got.ActionSteps[0].ActionType);
        Assert.Equal(42, got.PriorityScore);
    }

    [Fact]
    public async Task Links_list_in_deterministic_order()
    {
        InMemoryProductLearningPlanningRepository repo = new();
        ProductLearningScope scope = Scope();

        await repo.InsertThemeAsync(
            new ProductLearningImprovementThemeRecord
            {
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                ThemeKey = "k",
                Title = "T",
                Summary = "S",
                AffectedArtifactTypeOrWorkflowArea = "A",
                SeverityBand = "Low",
                EvidenceSignalCount = 1,
                DistinctRunCount = 1,
                DerivationRuleVersion = "59R-v1"
            },
            CancellationToken.None);

        Guid themeId = (await repo.ListThemesAsync(scope, 1, CancellationToken.None))[0].ThemeId;

        await repo.InsertPlanAsync(
            new ProductLearningImprovementPlanRecord
            {
                ThemeId = themeId,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                Title = "Plan",
                Summary = "P",
                ActionSteps =
                [
                    new ProductLearningImprovementPlanActionStep
                    {
                        Ordinal = 1,
                        ActionType = "T",
                        Description = "D"
                    }
                ],
                PriorityScore = 1
            },
            CancellationToken.None);

        Guid planId = (await repo.ListPlansAsync(scope, 1, CancellationToken.None))[0].PlanId;

        await repo.AddPlanArchitectureRunLinkAsync(
            new ProductLearningImprovementPlanRunLinkRecord { PlanId = planId, ArchitectureRunId = "z-run" },
            CancellationToken.None);

        await repo.AddPlanArchitectureRunLinkAsync(
            new ProductLearningImprovementPlanRunLinkRecord { PlanId = planId, ArchitectureRunId = "a-run" },
            CancellationToken.None);

        IReadOnlyList<string> runs = await repo.ListPlanArchitectureRunIdsAsync(planId, scope, CancellationToken.None);

        Assert.Equal(new[] { "a-run", "z-run" }, runs);

        Guid s1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid s2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        await repo.AddPlanSignalLinkAsync(
            new ProductLearningImprovementPlanSignalLinkRecord
            {
                PlanId = planId,
                SignalId = s2,
                TriageStatusSnapshot = ProductLearningTriageStatusValues.Open
            },
            CancellationToken.None);

        await repo.AddPlanSignalLinkAsync(
            new ProductLearningImprovementPlanSignalLinkRecord { PlanId = planId, SignalId = s1 },
            CancellationToken.None);

        IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord> sigs =
            await repo.ListPlanSignalLinksAsync(planId, scope, CancellationToken.None);

        Assert.Equal(s1, sigs[0].SignalId);
        Assert.Equal(s2, sigs[1].SignalId);
        Assert.Equal(ProductLearningTriageStatusValues.Open, sigs[1].TriageStatusSnapshot);

        await repo.AddPlanArtifactLinkAsync(
            new ProductLearningImprovementPlanArtifactLinkRecord
            {
                PlanId = planId,
                PilotArtifactHint = "diagram.png"
            },
            CancellationToken.None);

        IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord> arts =
            await repo.ListPlanArtifactLinksAsync(planId, scope, CancellationToken.None);

        Assert.Single(arts);
        Assert.Equal("diagram.png", arts[0].PilotArtifactHint);
    }

    [Fact]
    public void Action_step_validation_rejects_duplicates_and_overflow()
    {
        InMemoryProductLearningPlanningRepository repo = new();
        ProductLearningScope scope = Scope();

        List<ProductLearningImprovementPlanActionStep> dupOrd =
        [
            new ProductLearningImprovementPlanActionStep
            {
                Ordinal = 1,
                ActionType = "A",
                Description = "d"
            },
            new ProductLearningImprovementPlanActionStep
            {
                Ordinal = 1,
                ActionType = "B",
                Description = "d2"
            }
        ];

        Assert.Throws<ArgumentException>(() =>
            ProductLearningPlanningRepositoryValidation.EnsureActionSteps(dupOrd));

        List<ProductLearningImprovementPlanActionStep> tooMany =
            Enumerable.Range(1, 21)
                .Select(i => new ProductLearningImprovementPlanActionStep
                {
                    Ordinal = i,
                    ActionType = "T",
                    Description = "D"
                })
                .ToList();

        Assert.Throws<ArgumentException>(() =>
            ProductLearningPlanningRepositoryValidation.EnsureActionSteps(tooMany));
    }
}
