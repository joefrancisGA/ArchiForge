using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;

namespace ArchLucid.Persistence.Tests.ProductLearning.Planning;

/// <summary>
///     Direct coverage for <see cref="ProductLearningPlanningRepositoryValidation" /> (59R planning persistence guards).
/// </summary>
[Trait("ChangeSet", "59R")]
public sealed class ProductLearningPlanningRepositoryValidationTests
{
    private static ProductLearningScope ValidScope() =>
        new()
        {
            TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        };

    private static ProductLearningImprovementThemeRecord MinimalValidTheme(ProductLearningScope scope) =>
        new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ThemeKey = "k",
            Title = "T",
            Summary = "S",
            AffectedArtifactTypeOrWorkflowArea = "Area",
            SeverityBand = "Low",
            EvidenceSignalCount = 0,
            DistinctRunCount = 0,
            DerivationRuleVersion = "v1",
        };

    [Fact]
    public void EnsureScope_throws_when_scope_null()
    {
        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureScope(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public void EnsureTake_throws_out_of_range(int take)
    {
        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureTake(take);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(250)]
    [InlineData(500)]
    public void EnsureTake_accepts_inclusive_bounds(int take)
    {
        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureTake(take);

        act.Should().NotThrow();
    }

    [Fact]
    public void NormalizeThemeStatus_whitespace_defaults_to_proposed()
    {
        string r = ProductLearningPlanningRepositoryValidation.NormalizeThemeStatus("  ");

        r.Should().Be(ProductLearningImprovementThemeStatusValues.Proposed);
    }

    [Fact]
    public void NormalizeThemeStatus_accepts_known_statuses()
    {
        ProductLearningPlanningRepositoryValidation
            .NormalizeThemeStatus(ProductLearningImprovementThemeStatusValues.Accepted)
            .Should()
            .Be(ProductLearningImprovementThemeStatusValues.Accepted);
    }

    [Fact]
    public void NormalizeThemeStatus_rejects_unknown()
    {
        Action act = () => ProductLearningPlanningRepositoryValidation.NormalizeThemeStatus("Unknown");

        act.Should().Throw<ArgumentException>().WithParameterName("status");
    }

    [Fact]
    public void NormalizePlanStatus_whitespace_defaults_to_proposed()
    {
        string r = ProductLearningPlanningRepositoryValidation.NormalizePlanStatus("\t");

        r.Should().Be(ProductLearningImprovementPlanStatusValues.Proposed);
    }

    [Fact]
    public void NormalizePlanStatus_accepts_under_review()
    {
        ProductLearningPlanningRepositoryValidation
            .NormalizePlanStatus(ProductLearningImprovementPlanStatusValues.UnderReview)
            .Should()
            .Be(ProductLearningImprovementPlanStatusValues.UnderReview);
    }

    [Fact]
    public void NormalizePlanStatus_rejects_unknown()
    {
        Action act = () => ProductLearningPlanningRepositoryValidation.NormalizePlanStatus("Draft");

        act.Should().Throw<ArgumentException>().WithParameterName("status");
    }

    [Fact]
    public void EnsureTheme_throws_when_theme_null()
    {
        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureTheme(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsureTheme_throws_when_tenant_missing()
    {
        ProductLearningScope scope = ValidScope();
        ProductLearningImprovementThemeRecord baseTheme = MinimalValidTheme(scope);
        ProductLearningImprovementThemeRecord theme = new()
        {
            TenantId = Guid.Empty,
            WorkspaceId = baseTheme.WorkspaceId,
            ProjectId = baseTheme.ProjectId,
            ThemeKey = baseTheme.ThemeKey,
            Title = baseTheme.Title,
            Summary = baseTheme.Summary,
            AffectedArtifactTypeOrWorkflowArea = baseTheme.AffectedArtifactTypeOrWorkflowArea,
            SeverityBand = baseTheme.SeverityBand,
            EvidenceSignalCount = baseTheme.EvidenceSignalCount,
            DistinctRunCount = baseTheme.DistinctRunCount,
            DerivationRuleVersion = baseTheme.DerivationRuleVersion,
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureTheme(theme);

        act.Should().Throw<ArgumentException>().WithParameterName("theme");
    }

    [Fact]
    public void EnsureTheme_throws_when_theme_key_too_long()
    {
        ProductLearningScope scope = ValidScope();
        ProductLearningImprovementThemeRecord baseTheme = MinimalValidTheme(scope);
        ProductLearningImprovementThemeRecord theme = new()
        {
            TenantId = baseTheme.TenantId,
            WorkspaceId = baseTheme.WorkspaceId,
            ProjectId = baseTheme.ProjectId,
            ThemeKey = new string('x', ProductLearningPlanningRepositoryValidation.MaxThemeKeyLength + 1),
            Title = baseTheme.Title,
            Summary = baseTheme.Summary,
            AffectedArtifactTypeOrWorkflowArea = baseTheme.AffectedArtifactTypeOrWorkflowArea,
            SeverityBand = baseTheme.SeverityBand,
            EvidenceSignalCount = baseTheme.EvidenceSignalCount,
            DistinctRunCount = baseTheme.DistinctRunCount,
            DerivationRuleVersion = baseTheme.DerivationRuleVersion,
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureTheme(theme);

        act.Should().Throw<ArgumentException>().WithParameterName("theme");
    }

    [Fact]
    public void EnsureTheme_throws_when_title_too_long()
    {
        ProductLearningScope scope = ValidScope();
        ProductLearningImprovementThemeRecord baseTheme = MinimalValidTheme(scope);
        ProductLearningImprovementThemeRecord theme = new()
        {
            TenantId = baseTheme.TenantId,
            WorkspaceId = baseTheme.WorkspaceId,
            ProjectId = baseTheme.ProjectId,
            ThemeKey = baseTheme.ThemeKey,
            Title = new string('a', ProductLearningPlanningRepositoryValidation.MaxTitleLength + 1),
            Summary = baseTheme.Summary,
            AffectedArtifactTypeOrWorkflowArea = baseTheme.AffectedArtifactTypeOrWorkflowArea,
            SeverityBand = baseTheme.SeverityBand,
            EvidenceSignalCount = baseTheme.EvidenceSignalCount,
            DistinctRunCount = baseTheme.DistinctRunCount,
            DerivationRuleVersion = baseTheme.DerivationRuleVersion,
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureTheme(theme);

        act.Should().Throw<ArgumentException>().WithParameterName("theme");
    }

    [Fact]
    public void EnsureTheme_throws_when_derivation_rule_version_too_long()
    {
        ProductLearningScope scope = ValidScope();
        ProductLearningImprovementThemeRecord baseTheme = MinimalValidTheme(scope);
        ProductLearningImprovementThemeRecord theme = new()
        {
            TenantId = baseTheme.TenantId,
            WorkspaceId = baseTheme.WorkspaceId,
            ProjectId = baseTheme.ProjectId,
            ThemeKey = baseTheme.ThemeKey,
            Title = baseTheme.Title,
            Summary = baseTheme.Summary,
            AffectedArtifactTypeOrWorkflowArea = baseTheme.AffectedArtifactTypeOrWorkflowArea,
            SeverityBand = baseTheme.SeverityBand,
            EvidenceSignalCount = baseTheme.EvidenceSignalCount,
            DistinctRunCount = baseTheme.DistinctRunCount,
            DerivationRuleVersion = new string(
                'z',
                ProductLearningPlanningRepositoryValidation.MaxDerivationRuleVersionLength + 1),
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureTheme(theme);

        act.Should().Throw<ArgumentException>().WithParameterName("theme");
    }

    [Fact]
    public void EnsureTheme_throws_when_counts_negative()
    {
        ProductLearningScope scope = ValidScope();
        ProductLearningImprovementThemeRecord baseTheme = MinimalValidTheme(scope);
        ProductLearningImprovementThemeRecord theme = new()
        {
            TenantId = baseTheme.TenantId,
            WorkspaceId = baseTheme.WorkspaceId,
            ProjectId = baseTheme.ProjectId,
            ThemeKey = baseTheme.ThemeKey,
            Title = baseTheme.Title,
            Summary = baseTheme.Summary,
            AffectedArtifactTypeOrWorkflowArea = baseTheme.AffectedArtifactTypeOrWorkflowArea,
            SeverityBand = baseTheme.SeverityBand,
            EvidenceSignalCount = -1,
            DistinctRunCount = baseTheme.DistinctRunCount,
            DerivationRuleVersion = baseTheme.DerivationRuleVersion,
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureTheme(theme);

        act.Should().Throw<ArgumentException>().WithParameterName("theme");
    }

    [Fact]
    public void EnsureTheme_throws_when_status_unknown()
    {
        ProductLearningScope scope = ValidScope();
        ProductLearningImprovementThemeRecord baseTheme = MinimalValidTheme(scope);
        ProductLearningImprovementThemeRecord theme = new()
        {
            TenantId = baseTheme.TenantId,
            WorkspaceId = baseTheme.WorkspaceId,
            ProjectId = baseTheme.ProjectId,
            ThemeKey = baseTheme.ThemeKey,
            Title = baseTheme.Title,
            Summary = baseTheme.Summary,
            AffectedArtifactTypeOrWorkflowArea = baseTheme.AffectedArtifactTypeOrWorkflowArea,
            SeverityBand = baseTheme.SeverityBand,
            EvidenceSignalCount = baseTheme.EvidenceSignalCount,
            DistinctRunCount = baseTheme.DistinctRunCount,
            DerivationRuleVersion = baseTheme.DerivationRuleVersion,
            Status = "Retired",
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureTheme(theme);

        act.Should().Throw<ArgumentException>().WithParameterName("status");
    }

    [Fact]
    public void EnsureTheme_accepts_valid_theme()
    {
        ProductLearningScope scope = ValidScope();
        ProductLearningImprovementThemeRecord theme = MinimalValidTheme(scope);

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureTheme(theme);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsurePlan_throws_when_plan_null()
    {
        Action act = () => ProductLearningPlanningRepositoryValidation.EnsurePlan(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsurePlan_throws_when_theme_id_empty()
    {
        ProductLearningScope scope = ValidScope();
        ProductLearningImprovementPlanRecord plan = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ThemeId = Guid.Empty,
            Title = "P",
            Summary = "S",
            ActionSteps = [new ProductLearningImprovementPlanActionStep { Ordinal = 1, ActionType = "T", Description = "D" }],
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsurePlan(plan);

        act.Should().Throw<ArgumentException>().WithParameterName("plan");
    }

    [Fact]
    public void EnsurePlan_throws_when_status_invalid()
    {
        ProductLearningScope scope = ValidScope();
        ProductLearningImprovementPlanRecord plan = new()
        {
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            ThemeId = Guid.NewGuid(),
            Title = "P",
            Summary = "S",
            Status = "Unknown",
            ActionSteps = [new ProductLearningImprovementPlanActionStep { Ordinal = 1, ActionType = "T", Description = "D" }],
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsurePlan(plan);

        act.Should().Throw<ArgumentException>().WithParameterName("status");
    }

    [Fact]
    public void EnsureActionSteps_throws_when_null()
    {
        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureActionSteps(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsureActionSteps_throws_when_empty_list()
    {
        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureActionSteps([]);

        act.Should().Throw<ArgumentException>().WithParameterName("steps");
    }

    [Fact]
    public void EnsureActionSteps_throws_when_step_null()
    {
        List<ProductLearningImprovementPlanActionStep> steps = [];
        steps.Add(null!);

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureActionSteps(steps);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsureRunLink_throws_when_plan_id_empty()
    {
        ProductLearningImprovementPlanRunLinkRecord link = new()
        {
            PlanId = Guid.Empty,
            ArchitectureRunId = "abc"
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureRunLink(link);

        act.Should().Throw<ArgumentException>().WithParameterName("link");
    }

    [Fact]
    public void EnsureRunLink_throws_when_run_id_blank()
    {
        ProductLearningImprovementPlanRunLinkRecord link = new()
        {
            PlanId = Guid.NewGuid(),
            ArchitectureRunId = "  "
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureRunLink(link);

        act.Should().Throw<ArgumentException>().WithParameterName("link");
    }

    [Fact]
    public void EnsureRunLink_accepts_valid()
    {
        ProductLearningImprovementPlanRunLinkRecord link = new()
        {
            PlanId = Guid.NewGuid(),
            ArchitectureRunId = "00000000000000000000000000000001",
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureRunLink(link);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureSignalLink_throws_when_triage_snapshot_invalid()
    {
        ProductLearningImprovementPlanSignalLinkRecord link = new()
        {
            PlanId = Guid.NewGuid(),
            SignalId = Guid.NewGuid(),
            TriageStatusSnapshot = "Unknown",
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureSignalLink(link);

        act.Should().Throw<ArgumentException>().WithParameterName("snapshot");
    }

    [Fact]
    public void EnsureSignalLink_accepts_open_snapshot()
    {
        ProductLearningImprovementPlanSignalLinkRecord link = new()
        {
            PlanId = Guid.NewGuid(),
            SignalId = Guid.NewGuid(),
            TriageStatusSnapshot = ProductLearningTriageStatusValues.Open,
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureSignalLink(link);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureArtifactLink_throws_when_authority_partial()
    {
        ProductLearningImprovementPlanArtifactLinkRecord link = new()
        {
            PlanId = Guid.NewGuid(),
            AuthorityBundleId = Guid.NewGuid(),
            AuthorityArtifactSortOrder = null,
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureArtifactLink(link);

        act.Should().Throw<ArgumentException>().WithParameterName("link");
    }

    [Fact]
    public void EnsureArtifactLink_accepts_authority_pair()
    {
        ProductLearningImprovementPlanArtifactLinkRecord link = new()
        {
            PlanId = Guid.NewGuid(),
            AuthorityBundleId = Guid.NewGuid(),
            AuthorityArtifactSortOrder = 1,
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureArtifactLink(link);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureArtifactLink_accepts_pilot_hint_only()
    {
        ProductLearningImprovementPlanArtifactLinkRecord link = new()
        {
            PlanId = Guid.NewGuid(),
            PilotArtifactHint = "artifact.zip",
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureArtifactLink(link);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureArtifactLink_throws_when_hint_too_long()
    {
        ProductLearningImprovementPlanArtifactLinkRecord link = new()
        {
            PlanId = Guid.NewGuid(),
            PilotArtifactHint = new string('h', ProductLearningPlanningRepositoryValidation.MaxArtifactHintLength + 1),
        };

        Action act = () => ProductLearningPlanningRepositoryValidation.EnsureArtifactLink(link);

        act.Should().Throw<ArgumentException>().WithParameterName("link");
    }
}
