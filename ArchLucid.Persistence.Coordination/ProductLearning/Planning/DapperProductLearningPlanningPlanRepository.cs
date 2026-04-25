using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Coordination.ProductLearning.Planning;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
internal sealed class DapperProductLearningPlanningPlanRepository(ISqlConnectionFactory connectionFactory)
{
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
                    plan.PriorityExplanation,
                    Status = status,
                    CreatedUtc = createdUtc,
                    plan.CreatedByUserId
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
        ProductLearningImprovementPlanSqlRow? row =
            await connection.QuerySingleOrDefaultAsync<ProductLearningImprovementPlanSqlRow>(
                new CommandDefinition(
                    sql,
                    new { PlanId = planId, scope.TenantId, scope.WorkspaceId, scope.ProjectId },
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
        IEnumerable<ProductLearningImprovementPlanSqlRow> rows =
            await connection.QueryAsync<ProductLearningImprovementPlanSqlRow>(
                new CommandDefinition(
                    sql,
                    new { Take = take, scope.TenantId, scope.WorkspaceId, scope.ProjectId },
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
        IEnumerable<ProductLearningImprovementPlanSqlRow> rows =
            await connection.QueryAsync<ProductLearningImprovementPlanSqlRow>(
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
            throw new InvalidOperationException("Theme not found for ThemeId=" + themeId + ".");


        if (row.TenantId != plan.TenantId || row.WorkspaceId != plan.WorkspaceId || row.ProjectId != plan.ProjectId)

            throw new InvalidOperationException("Plan scope must match the parent theme scope.");
    }
}
