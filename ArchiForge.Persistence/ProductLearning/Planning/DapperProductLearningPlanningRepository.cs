using System.Diagnostics.CodeAnalysis;

using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Contracts.ProductLearning.Planning;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.ProductLearning.Planning;

/// <summary>Dapper access to 59R planning bridge tables.</summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperProductLearningPlanningRepository(ISqlConnectionFactory connectionFactory)
    : IProductLearningPlanningRepository
{
    public async Task InsertThemeAsync(ProductLearningImprovementThemeRecord theme, CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureTheme(theme);

        Guid themeId = theme.ThemeId == Guid.Empty ? Guid.NewGuid() : theme.ThemeId;
        DateTime createdUtc = theme.CreatedUtc == default ? DateTime.UtcNow : theme.CreatedUtc;
        string status = ProductLearningPlanningRepositoryValidation.NormalizeThemeStatus(theme.Status);

        const string sql = """
            INSERT INTO dbo.ProductLearningImprovementThemes
            (
                ThemeId,
                TenantId,
                WorkspaceId,
                ProjectId,
                ThemeKey,
                SourceAggregateKey,
                PatternKey,
                Title,
                Summary,
                AffectedArtifactTypeOrWorkflowArea,
                SeverityBand,
                EvidenceSignalCount,
                DistinctRunCount,
                AverageTrustScore,
                DerivationRuleVersion,
                Status,
                CreatedUtc,
                CreatedByUserId
            )
            VALUES
            (
                @ThemeId,
                @TenantId,
                @WorkspaceId,
                @ProjectId,
                @ThemeKey,
                @SourceAggregateKey,
                @PatternKey,
                @Title,
                @Summary,
                @AffectedArtifactTypeOrWorkflowArea,
                @SeverityBand,
                @EvidenceSignalCount,
                @DistinctRunCount,
                @AverageTrustScore,
                @DerivationRuleVersion,
                @Status,
                @CreatedUtc,
                @CreatedByUserId
            );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    ThemeId = themeId,
                    theme.TenantId,
                    theme.WorkspaceId,
                    theme.ProjectId,
                    theme.ThemeKey,
                    SourceAggregateKey = theme.SourceAggregateKey,
                    PatternKey = theme.PatternKey,
                    theme.Title,
                    theme.Summary,
                    theme.AffectedArtifactTypeOrWorkflowArea,
                    theme.SeverityBand,
                    theme.EvidenceSignalCount,
                    theme.DistinctRunCount,
                    AverageTrustScore = theme.AverageTrustScore,
                    theme.DerivationRuleVersion,
                    Status = status,
                    CreatedUtc = createdUtc,
                    CreatedByUserId = theme.CreatedByUserId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<ProductLearningImprovementThemeRecord?> GetThemeAsync(
        Guid themeId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        const string sql = """
            SELECT
                ThemeId,
                TenantId,
                WorkspaceId,
                ProjectId,
                ThemeKey,
                SourceAggregateKey,
                PatternKey,
                Title,
                Summary,
                AffectedArtifactTypeOrWorkflowArea,
                SeverityBand,
                EvidenceSignalCount,
                DistinctRunCount,
                AverageTrustScore,
                DerivationRuleVersion,
                Status,
                CreatedUtc,
                CreatedByUserId
            FROM dbo.ProductLearningImprovementThemes
            WHERE ThemeId = @ThemeId
              AND TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        ProductLearningImprovementThemeSqlRow? row = await connection.QuerySingleOrDefaultAsync<ProductLearningImprovementThemeSqlRow>(
            new CommandDefinition(
                sql,
                new
                {
                    ThemeId = themeId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId
                },
                cancellationToken: cancellationToken));

        return row is null ? null : MapTheme(row);
    }

    public async Task<IReadOnlyList<ProductLearningImprovementThemeRecord>> ListThemesAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);
        ProductLearningPlanningRepositoryValidation.EnsureTake(take);

        const string sql = """
            SELECT TOP (@Take)
                ThemeId,
                TenantId,
                WorkspaceId,
                ProjectId,
                ThemeKey,
                SourceAggregateKey,
                PatternKey,
                Title,
                Summary,
                AffectedArtifactTypeOrWorkflowArea,
                SeverityBand,
                EvidenceSignalCount,
                DistinctRunCount,
                AverageTrustScore,
                DerivationRuleVersion,
                Status,
                CreatedUtc,
                CreatedByUserId
            FROM dbo.ProductLearningImprovementThemes
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC, ThemeId ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<ProductLearningImprovementThemeSqlRow> rows = await connection.QueryAsync<ProductLearningImprovementThemeSqlRow>(
            new CommandDefinition(
                sql,
                new
                {
                    Take = take,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId
                },
                cancellationToken: cancellationToken));

        return rows.Select(static r => MapTheme(r)).ToList();
    }

    public async Task InsertPlanAsync(ProductLearningImprovementPlanRecord plan, CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsurePlan(plan);

        string status = ProductLearningPlanningRepositoryValidation.NormalizePlanStatus(plan.Status);
        string actionsJson = ProductLearningPlanningJsonSerializer.SerializeActionSteps(plan.ActionSteps);
        Guid planId = plan.PlanId == Guid.Empty ? Guid.NewGuid() : plan.PlanId;
        DateTime createdUtc = plan.CreatedUtc == default ? DateTime.UtcNow : plan.CreatedUtc;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await EnsureThemeScopeMatchesAsync(connection, plan.ThemeId, plan, cancellationToken);

        const string sql = """
            INSERT INTO dbo.ProductLearningImprovementPlans
            (
                PlanId,
                TenantId,
                WorkspaceId,
                ProjectId,
                ThemeId,
                Title,
                Summary,
                BoundedActionsJson,
                PriorityScore,
                PriorityExplanation,
                Status,
                CreatedUtc,
                CreatedByUserId
            )
            VALUES
            (
                @PlanId,
                @TenantId,
                @WorkspaceId,
                @ProjectId,
                @ThemeId,
                @Title,
                @Summary,
                @BoundedActionsJson,
                @PriorityScore,
                @PriorityExplanation,
                @Status,
                @CreatedUtc,
                @CreatedByUserId
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    PlanId = planId,
                    plan.TenantId,
                    plan.WorkspaceId,
                    plan.ProjectId,
                    plan.ThemeId,
                    plan.Title,
                    plan.Summary,
                    BoundedActionsJson = actionsJson,
                    plan.PriorityScore,
                    PriorityExplanation = plan.PriorityExplanation,
                    Status = status,
                    CreatedUtc = createdUtc,
                    CreatedByUserId = plan.CreatedByUserId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<ProductLearningImprovementPlanRecord?> GetPlanAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        const string sql = """
            SELECT
                PlanId,
                TenantId,
                WorkspaceId,
                ProjectId,
                ThemeId,
                Title,
                Summary,
                BoundedActionsJson,
                PriorityScore,
                PriorityExplanation,
                Status,
                CreatedUtc,
                CreatedByUserId
            FROM dbo.ProductLearningImprovementPlans
            WHERE PlanId = @PlanId
              AND TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        ProductLearningImprovementPlanSqlRow? row = await connection.QuerySingleOrDefaultAsync<ProductLearningImprovementPlanSqlRow>(
            new CommandDefinition(
                sql,
                new
                {
                    PlanId = planId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId
                },
                cancellationToken: cancellationToken));

        return row is null ? null : MapPlan(row);
    }

    public async Task<IReadOnlyList<ProductLearningImprovementPlanRecord>> ListPlansAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);
        ProductLearningPlanningRepositoryValidation.EnsureTake(take);

        const string sql = """
            SELECT TOP (@Take)
                PlanId,
                TenantId,
                WorkspaceId,
                ProjectId,
                ThemeId,
                Title,
                Summary,
                BoundedActionsJson,
                PriorityScore,
                PriorityExplanation,
                Status,
                CreatedUtc,
                CreatedByUserId
            FROM dbo.ProductLearningImprovementPlans
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC, PlanId ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<ProductLearningImprovementPlanSqlRow> rows = await connection.QueryAsync<ProductLearningImprovementPlanSqlRow>(
            new CommandDefinition(
                sql,
                new
                {
                    Take = take,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId
                },
                cancellationToken: cancellationToken));

        return rows.Select(static r => MapPlan(r)).ToList();
    }

    public async Task<IReadOnlyList<ProductLearningImprovementPlanRecord>> ListPlansForThemeAsync(
        Guid themeId,
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);
        ProductLearningPlanningRepositoryValidation.EnsureTake(take);

        const string sql = """
            SELECT TOP (@Take)
                PlanId,
                TenantId,
                WorkspaceId,
                ProjectId,
                ThemeId,
                Title,
                Summary,
                BoundedActionsJson,
                PriorityScore,
                PriorityExplanation,
                Status,
                CreatedUtc,
                CreatedByUserId
            FROM dbo.ProductLearningImprovementPlans
            WHERE ThemeId = @ThemeId
              AND TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC, PlanId ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<ProductLearningImprovementPlanSqlRow> rows = await connection.QueryAsync<ProductLearningImprovementPlanSqlRow>(
            new CommandDefinition(
                sql,
                new
                {
                    ThemeId = themeId,
                    Take = take,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId
                },
                cancellationToken: cancellationToken));

        return rows.Select(static r => MapPlan(r)).ToList();
    }

    public async Task AddPlanArchitectureRunLinkAsync(
        ProductLearningImprovementPlanRunLinkRecord link,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureRunLink(link);

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await RequirePlanScopeAsync(connection, link.PlanId, cancellationToken);

        await RequireArchitectureRunExistsAsync(connection, link.ArchitectureRunId, cancellationToken);

        const string sql = """
            INSERT INTO dbo.ProductLearningImprovementPlanArchitectureRuns (PlanId, ArchitectureRunId)
            VALUES (@PlanId, @ArchitectureRunId);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { link.PlanId, link.ArchitectureRunId },
                cancellationToken: cancellationToken));
    }

    public async Task AddPlanSignalLinkAsync(
        ProductLearningImprovementPlanSignalLinkRecord link,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureSignalLink(link);

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        ProductLearningScope scope = await RequirePlanScopeAsync(connection, link.PlanId, cancellationToken);

        await RequirePilotSignalInScopeAsync(connection, link.SignalId, scope, cancellationToken);

        const string sql = """
            INSERT INTO dbo.ProductLearningImprovementPlanSignalLinks (PlanId, SignalId, TriageStatusSnapshot)
            VALUES (@PlanId, @SignalId, @TriageStatusSnapshot);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    link.PlanId,
                    link.SignalId,
                    TriageStatusSnapshot = link.TriageStatusSnapshot
                },
                cancellationToken: cancellationToken));
    }

    public async Task AddPlanArtifactLinkAsync(
        ProductLearningImprovementPlanArtifactLinkRecord link,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureArtifactLink(link);

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        ProductLearningScope scope = await RequirePlanScopeAsync(connection, link.PlanId, cancellationToken);

        Guid linkId = link.LinkId == Guid.Empty ? Guid.NewGuid() : link.LinkId;

        if (link.AuthorityBundleId is not null && link.AuthorityArtifactSortOrder is not null)
        {
            await RequireAuthorityArtifactInScopeAsync(
                connection,
                link.AuthorityBundleId.Value,
                link.AuthorityArtifactSortOrder.Value,
                scope,
                cancellationToken);
        }

        const string sql = """
            INSERT INTO dbo.ProductLearningImprovementPlanArtifactLinks
            (
                LinkId,
                PlanId,
                AuthorityBundleId,
                AuthorityArtifactSortOrder,
                PilotArtifactHint
            )
            VALUES
            (
                @LinkId,
                @PlanId,
                @AuthorityBundleId,
                @AuthorityArtifactSortOrder,
                @PilotArtifactHint
            );
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    LinkId = linkId,
                    link.PlanId,
                    AuthorityBundleId = link.AuthorityBundleId,
                    AuthorityArtifactSortOrder = link.AuthorityArtifactSortOrder,
                    PilotArtifactHint = link.PilotArtifactHint
                },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<string>> ListPlanArchitectureRunIdsAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        const string sql = """
            SELECT r.ArchitectureRunId
            FROM dbo.ProductLearningImprovementPlanArchitectureRuns r
            INNER JOIN dbo.ProductLearningImprovementPlans p ON p.PlanId = r.PlanId
            WHERE r.PlanId = @PlanId
              AND p.TenantId = @TenantId
              AND p.WorkspaceId = @WorkspaceId
              AND p.ProjectId = @ProjectId
            ORDER BY r.ArchitectureRunId ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<string> ids = await connection.QueryAsync<string>(
            new CommandDefinition(
                sql,
                new
                {
                    PlanId = planId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId
                },
                cancellationToken: cancellationToken));

        return ids.ToList();
    }

    public async Task<IReadOnlyList<ProductLearningImprovementPlanSignalLinkRecord>> ListPlanSignalLinksAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        const string sql = """
            SELECT s.PlanId, s.SignalId, s.TriageStatusSnapshot
            FROM dbo.ProductLearningImprovementPlanSignalLinks s
            INNER JOIN dbo.ProductLearningImprovementPlans p ON p.PlanId = s.PlanId
            WHERE s.PlanId = @PlanId
              AND p.TenantId = @TenantId
              AND p.WorkspaceId = @WorkspaceId
              AND p.ProjectId = @ProjectId
            ORDER BY s.SignalId ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<ProductLearningImprovementPlanSignalLinkSqlRow> rows =
            await connection.QueryAsync<ProductLearningImprovementPlanSignalLinkSqlRow>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        PlanId = planId,
                        scope.TenantId,
                        scope.WorkspaceId,
                        scope.ProjectId
                    },
                    cancellationToken: cancellationToken));

        return rows
            .Select(static r => new ProductLearningImprovementPlanSignalLinkRecord
            {
                PlanId = r.PlanId,
                SignalId = r.SignalId,
                TriageStatusSnapshot = r.TriageStatusSnapshot
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ProductLearningImprovementPlanArtifactLinkRecord>> ListPlanArtifactLinksAsync(
        Guid planId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureScope(scope);

        const string sql = """
            SELECT a.LinkId, a.PlanId, a.AuthorityBundleId, a.AuthorityArtifactSortOrder, a.PilotArtifactHint
            FROM dbo.ProductLearningImprovementPlanArtifactLinks a
            INNER JOIN dbo.ProductLearningImprovementPlans p ON p.PlanId = a.PlanId
            WHERE a.PlanId = @PlanId
              AND p.TenantId = @TenantId
              AND p.WorkspaceId = @WorkspaceId
              AND p.ProjectId = @ProjectId
            ORDER BY a.LinkId ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<ProductLearningImprovementPlanArtifactLinkSqlRow> rows =
            await connection.QueryAsync<ProductLearningImprovementPlanArtifactLinkSqlRow>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        PlanId = planId,
                        scope.TenantId,
                        scope.WorkspaceId,
                        scope.ProjectId
                    },
                    cancellationToken: cancellationToken));

        return rows
            .Select(static r => new ProductLearningImprovementPlanArtifactLinkRecord
            {
                LinkId = r.LinkId,
                PlanId = r.PlanId,
                AuthorityBundleId = r.AuthorityBundleId,
                AuthorityArtifactSortOrder = r.AuthorityArtifactSortOrder,
                PilotArtifactHint = r.PilotArtifactHint
            })
            .ToList();
    }

    private static ProductLearningImprovementThemeRecord MapTheme(ProductLearningImprovementThemeSqlRow row) =>
        new()
        {
            ThemeId = row.ThemeId,
            TenantId = row.TenantId,
            WorkspaceId = row.WorkspaceId,
            ProjectId = row.ProjectId,
            ThemeKey = row.ThemeKey,
            SourceAggregateKey = row.SourceAggregateKey,
            PatternKey = row.PatternKey,
            Title = row.Title,
            Summary = row.Summary,
            AffectedArtifactTypeOrWorkflowArea = row.AffectedArtifactTypeOrWorkflowArea,
            SeverityBand = row.SeverityBand,
            EvidenceSignalCount = row.EvidenceSignalCount,
            DistinctRunCount = row.DistinctRunCount,
            AverageTrustScore = row.AverageTrustScore,
            DerivationRuleVersion = row.DerivationRuleVersion,
            Status = row.Status,
            CreatedUtc = row.CreatedUtc,
            CreatedByUserId = row.CreatedByUserId
        };

    private static ProductLearningImprovementPlanRecord MapPlan(ProductLearningImprovementPlanSqlRow row)
    {
        IReadOnlyList<ProductLearningImprovementPlanActionStep> steps =
            ProductLearningPlanningJsonSerializer.DeserializeActionSteps(row.BoundedActionsJson);

        return new ProductLearningImprovementPlanRecord
        {
            PlanId = row.PlanId,
            TenantId = row.TenantId,
            WorkspaceId = row.WorkspaceId,
            ProjectId = row.ProjectId,
            ThemeId = row.ThemeId,
            Title = row.Title,
            Summary = row.Summary,
            ActionSteps = steps,
            PriorityScore = row.PriorityScore,
            PriorityExplanation = row.PriorityExplanation,
            Status = row.Status,
            CreatedUtc = row.CreatedUtc,
            CreatedByUserId = row.CreatedByUserId
        };
    }

    private static async Task EnsureThemeScopeMatchesAsync(
        SqlConnection connection,
        Guid themeId,
        ProductLearningImprovementPlanRecord plan,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TenantId, WorkspaceId, ProjectId
            FROM dbo.ProductLearningImprovementThemes
            WHERE ThemeId = @ThemeId;
            """;

        ProductLearningScopeSqlRow? row = await connection.QuerySingleOrDefaultAsync<ProductLearningScopeSqlRow>(
            new CommandDefinition(sql, new { ThemeId = themeId }, cancellationToken: cancellationToken));

        if (row is null)
        {
            throw new InvalidOperationException("Theme not found for ThemeId=" + themeId + ".");
        }

        if (row.TenantId != plan.TenantId || row.WorkspaceId != plan.WorkspaceId || row.ProjectId != plan.ProjectId)
        {
            throw new InvalidOperationException("Plan scope must match the parent theme scope.");
        }
    }

    private static async Task<ProductLearningScope> RequirePlanScopeAsync(
        SqlConnection connection,
        Guid planId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TenantId, WorkspaceId, ProjectId
            FROM dbo.ProductLearningImprovementPlans
            WHERE PlanId = @PlanId;
            """;

        ProductLearningScopeSqlRow? row = await connection.QuerySingleOrDefaultAsync<ProductLearningScopeSqlRow>(
            new CommandDefinition(sql, new { PlanId = planId }, cancellationToken: cancellationToken));

        if (row is null)
        {
            throw new InvalidOperationException("Plan not found for PlanId=" + planId + ".");
        }

        return new ProductLearningScope
        {
            TenantId = row.TenantId,
            WorkspaceId = row.WorkspaceId,
            ProjectId = row.ProjectId
        };
    }

    private static async Task RequireArchitectureRunExistsAsync(
        SqlConnection connection,
        string architectureRunId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS(
                SELECT 1 FROM dbo.ArchitectureRuns WHERE RunId = @RunId) THEN 1 ELSE 0 END;
            """;

        int ok = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { RunId = architectureRunId }, cancellationToken: cancellationToken));

        if (ok == 0)
        {
            throw new InvalidOperationException("ArchitectureRuns.RunId was not found: " + architectureRunId);
        }
    }

    private static async Task RequirePilotSignalInScopeAsync(
        SqlConnection connection,
        Guid signalId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS(
                SELECT 1 FROM dbo.ProductLearningPilotSignals
                WHERE SignalId = @SignalId
                  AND TenantId = @TenantId
                  AND WorkspaceId = @WorkspaceId
                  AND ProjectId = @ProjectId) THEN 1 ELSE 0 END;
            """;

        int ok = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    SignalId = signalId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId
                },
                cancellationToken: cancellationToken));

        if (ok == 0)
        {
            throw new InvalidOperationException(
                "ProductLearningPilotSignals row was not found in the plan's scope for SignalId=" + signalId + ".");
        }
    }

    private static async Task RequireAuthorityArtifactInScopeAsync(
        SqlConnection connection,
        Guid bundleId,
        int sortOrder,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        if (await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT CASE WHEN OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NULL THEN 0 ELSE 1 END;
                    """,
                    cancellationToken: cancellationToken)) == 0)
        {
            return;
        }

        const string sql = """
            SELECT CASE WHEN EXISTS(
                SELECT 1
                FROM dbo.ArtifactBundleArtifacts aba
                INNER JOIN dbo.ArtifactBundles b ON b.BundleId = aba.BundleId
                WHERE aba.BundleId = @BundleId
                  AND aba.SortOrder = @SortOrder
                  AND b.TenantId = @TenantId
                  AND b.WorkspaceId = @WorkspaceId
                  AND b.ProjectId = @ProjectId) THEN 1 ELSE 0 END;
            """;

        int ok = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    BundleId = bundleId,
                    SortOrder = sortOrder,
                    scope.TenantId,
                    scope.WorkspaceId,
                    scope.ProjectId
                },
                cancellationToken: cancellationToken));

        if (ok == 0)
        {
            throw new InvalidOperationException(
                "Authority artifact coordinates were not found in the plan's scope (BundleId/SortOrder).");
        }
    }
}
