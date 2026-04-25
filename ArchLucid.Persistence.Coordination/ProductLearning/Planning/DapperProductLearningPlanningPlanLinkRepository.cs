using System.Diagnostics.CodeAnalysis;

using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Coordination.ProductLearning.Planning;

[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
internal sealed class DapperProductLearningPlanningPlanLinkRepository(ISqlConnectionFactory connectionFactory)
{
    public async Task AddPlanArchitectureRunLinkAsync(
        ProductLearningImprovementPlanRunLinkRecord link,
        CancellationToken cancellationToken)
    {
        ProductLearningPlanningRepositoryValidation.EnsureRunLink(link);

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        _ = await RequirePlanScopeAsync(connection, link.PlanId, cancellationToken);

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
                new { link.PlanId, link.SignalId, link.TriageStatusSnapshot },
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

            await RequireAuthorityArtifactInScopeAsync(
                connection,
                link.AuthorityBundleId.Value,
                link.AuthorityArtifactSortOrder.Value,
                scope,
                cancellationToken);


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
                    link.AuthorityBundleId,
                    link.AuthorityArtifactSortOrder,
                    link.PilotArtifactHint
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
                new { PlanId = planId, scope.TenantId, scope.WorkspaceId, scope.ProjectId },
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
                    new { PlanId = planId, scope.TenantId, scope.WorkspaceId, scope.ProjectId },
                    cancellationToken: cancellationToken));

        return rows
            .Select(static r => new ProductLearningImprovementPlanSignalLinkRecord
            {
                PlanId = r.PlanId, SignalId = r.SignalId, TriageStatusSnapshot = r.TriageStatusSnapshot
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
                    new { PlanId = planId, scope.TenantId, scope.WorkspaceId, scope.ProjectId },
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
            throw new InvalidOperationException("Plan not found for PlanId=" + planId + ".");


        return new ProductLearningScope
        {
            TenantId = row.TenantId, WorkspaceId = row.WorkspaceId, ProjectId = row.ProjectId
        };
    }

    private static async Task RequireArchitectureRunExistsAsync(
        SqlConnection connection,
        string architectureRunId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(architectureRunId, "N", out Guid runGuid))

            throw new InvalidOperationException(
                "ArchitectureRunId must be a 32-character hex run id (N format): " + architectureRunId);


        const string sql = """
                           SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.Runs WHERE RunId = @RunId) THEN 1 ELSE 0 END;
                           """;

        int ok = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { RunId = runGuid }, cancellationToken: cancellationToken));

        if (ok == 0)
            throw new InvalidOperationException("dbo.Runs.RunId was not found: " + architectureRunId);
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
                new { SignalId = signalId, scope.TenantId, scope.WorkspaceId, scope.ProjectId },
                cancellationToken: cancellationToken));

        if (ok == 0)

            throw new InvalidOperationException(
                "ProductLearningPilotSignals row was not found in the plan's scope for SignalId=" + signalId + ".");
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

            return;


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

            throw new InvalidOperationException(
                "Authority artifact coordinates were not found in the plan's scope (BundleId/SortOrder).");
    }
}
