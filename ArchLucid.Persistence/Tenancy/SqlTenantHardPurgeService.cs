using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tenancy;

/// <summary>
///     Hard-deletes tenant-scoped <c>dbo</c> rows in dependency-safe order. <c>dbo.AuditEvents</c> is intentionally
///     skipped.
/// </summary>
public sealed class SqlTenantHardPurgeService(ISqlConnectionFactory connectionFactory) : ITenantHardPurgeService
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task<TenantHardPurgeResult> PurgeTenantAsync(
        Guid tenantId,
        TenantHardPurgeOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        int totalDeleted = 0;

        using (SqlRowLevelSecurityBypassAmbient.Enter())
        {
            await using SqlConnection connection =
                await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            await ApplyBypassSessionAsync(connection, cancellationToken);

            if (options.DryRun)
            {
                await AccumulateDryRunCountsAsync(connection, tenantId, counts, cancellationToken);

                return new TenantHardPurgeResult { RowsDeleted = 0, RowCountsByTable = counts };
            }

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.AgentExecutionTraces WHERE TaskId IN (SELECT TaskId FROM dbo.AgentTasks WHERE TRY_CAST(RunId AS UNIQUEIDENTIFIER) IN (SELECT RunId FROM dbo.Runs WHERE TenantId = @TenantId))",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "AgentExecutionTraces",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.AgentResults WHERE TaskId IN (SELECT TaskId FROM dbo.AgentTasks WHERE TRY_CAST(RunId AS UNIQUEIDENTIFIER) IN (SELECT RunId FROM dbo.Runs WHERE TenantId = @TenantId))",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "AgentResults",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.AgentTasks WHERE TRY_CAST(RunId AS UNIQUEIDENTIFIER) IN (SELECT RunId FROM dbo.Runs WHERE TenantId = @TenantId)",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "AgentTasks",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.ArtifactBundles WHERE TenantId = @TenantId",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "ArtifactBundles",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.GoldenManifests WHERE TenantId = @TenantId",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "GoldenManifests",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.FindingsSnapshots WHERE RunId IN (SELECT RunId FROM dbo.Runs WHERE TenantId = @TenantId)",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "FindingsSnapshots",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.GraphSnapshots WHERE RunId IN (SELECT RunId FROM dbo.Runs WHERE TenantId = @TenantId)",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "GraphSnapshots",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.ContextSnapshots WHERE RunId IN (SELECT RunId FROM dbo.Runs WHERE TenantId = @TenantId)",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "ContextSnapshots",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.DecisioningTraces WHERE RunId IN (SELECT RunId FROM dbo.Runs WHERE TenantId = @TenantId)",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "DecisioningTraces",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                """
                DELETE TOP (@Cap) FROM dbo.ComparisonRecords
                WHERE TRY_CAST(LeftRunId AS UNIQUEIDENTIFIER) IN (SELECT RunId FROM dbo.Runs WHERE TenantId = @TenantId)
                   OR TRY_CAST(RightRunId AS UNIQUEIDENTIFIER) IN (SELECT RunId FROM dbo.Runs WHERE TenantId = @TenantId);
                """,
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "ComparisonRecords",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.Runs WHERE TenantId = @TenantId",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "Runs",
                cancellationToken);

            totalDeleted += await DeleteProductLearningPlanChildrenAsync(
                connection,
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                cancellationToken);

            totalDeleted += await DeleteTenantScopedTablesAsync(
                connection,
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.TenantLifecycleTransitions WHERE TenantId = @TenantId",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "TenantLifecycleTransitions",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.TenantWorkspaces WHERE TenantId = @TenantId",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "TenantWorkspaces",
                cancellationToken);

            totalDeleted += await DeleteLoopAsync(
                connection,
                "DELETE TOP (@Cap) FROM dbo.Tenants WHERE Id = @TenantId",
                tenantId,
                options.MaxRowsPerStatement,
                counts,
                "Tenants",
                cancellationToken);
        }

        return new TenantHardPurgeResult { RowsDeleted = totalDeleted, RowCountsByTable = counts };
    }

    private static async Task ApplyBypassSessionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @k, @v, @read_only;";
        cmd.Parameters.AddWithValue("@k", "al_rls_bypass");
        cmd.Parameters.AddWithValue("@v", 1);
        cmd.Parameters.AddWithValue("@read_only", 0);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task AccumulateDryRunCountsAsync(
        SqlConnection connection,
        Guid tenantId,
        Dictionary<string, int> counts,
        CancellationToken cancellationToken)
    {
        int traces = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*) FROM dbo.AgentExecutionTraces WHERE TaskId IN (
                  SELECT TaskId FROM dbo.AgentTasks WHERE TRY_CAST(RunId AS UNIQUEIDENTIFIER) IN (
                    SELECT RunId FROM dbo.Runs WHERE TenantId = @TenantId));
                """,
                new { TenantId = tenantId },
                cancellationToken: cancellationToken));

        counts["AgentExecutionTraces"] = traces;

        int tenants = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM dbo.Tenants WHERE Id = @TenantId;",
                new { TenantId = tenantId },
                cancellationToken: cancellationToken));

        counts["Tenants"] = tenants;
    }

    private static async Task<int> DeleteProductLearningPlanChildrenAsync(
        SqlConnection connection,
        Guid tenantId,
        int cap,
        Dictionary<string, int> counts,
        CancellationToken cancellationToken)
    {
        int total = 0;

        if (await TableExistsAsync(connection, "dbo.ProductLearningImprovementPlanArchitectureRuns", cancellationToken))

            total += await DeleteLoopAsync(
                connection,
                """
                DELETE TOP (@Cap) FROM dbo.ProductLearningImprovementPlanArchitectureRuns
                WHERE PlanId IN (SELECT PlanId FROM dbo.ProductLearningImprovementPlans WHERE TenantId = @TenantId);
                """,
                tenantId,
                cap,
                counts,
                "ProductLearningImprovementPlanArchitectureRuns",
                cancellationToken);


        if (await TableExistsAsync(connection, "dbo.ProductLearningImprovementPlanSignalLinks", cancellationToken))

            total += await DeleteLoopAsync(
                connection,
                """
                DELETE TOP (@Cap) FROM dbo.ProductLearningImprovementPlanSignalLinks
                WHERE PlanId IN (SELECT PlanId FROM dbo.ProductLearningImprovementPlans WHERE TenantId = @TenantId);
                """,
                tenantId,
                cap,
                counts,
                "ProductLearningImprovementPlanSignalLinks",
                cancellationToken);


        return total;
    }

    private static async Task<int> DeleteTenantScopedTablesAsync(
        SqlConnection connection,
        Guid tenantId,
        int cap,
        Dictionary<string, int> counts,
        CancellationToken cancellationToken)
    {
        int total = 0;
        string[] tenantTables =
        [
            "dbo.UsageEvents",
            "dbo.TenantExecDigestPreferences",
            "dbo.SentEmails",
            "dbo.BillingSubscriptions",
            "dbo.TenantTrialSeatOccupants",
            "dbo.IntegrationEventOutbox",
            "dbo.ArchitectureRunIdempotency",
            "dbo.ProvenanceSnapshots",
            "dbo.ConversationThreads",
            "dbo.RecommendationRecords",
            "dbo.RecommendationLearningProfiles",
            "dbo.AdvisoryScanSchedules",
            "dbo.AdvisoryScanExecutions",
            "dbo.ArchitectureDigests",
            "dbo.DigestSubscriptions",
            "dbo.DigestDeliveryAttempts",
            "dbo.AlertRules",
            "dbo.AlertRecords",
            "dbo.AlertRoutingSubscriptions",
            "dbo.AlertDeliveryAttempts",
            "dbo.CompositeAlertRules",
            "dbo.PolicyPacks",
            "dbo.PolicyPackAssignments",
            "dbo.PolicyPackChangeLog",
            "dbo.ProductLearningPilotSignals",
            "dbo.ProductLearningImprovementPlans",
            "dbo.ProductLearningImprovementThemes"
        ];

        foreach (string table in tenantTables)
        {
            if (!await TableExistsAsync(connection, table, cancellationToken))
                continue;


            string label = table.Replace("dbo.", string.Empty, StringComparison.Ordinal);
            string sql = $"DELETE TOP (@Cap) FROM {table} WHERE TenantId = @TenantId";
            total += await DeleteLoopAsync(connection, sql, tenantId, cap, counts, label, cancellationToken);
        }

        return total;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string qualifiedName,
        CancellationToken cancellationToken)
    {
        string name = qualifiedName.Split('.', 2)[1];

        const string sql = """
                           SELECT COUNT(*) FROM sys.tables t
                           WHERE t.schema_id = SCHEMA_ID(N'dbo') AND t.name = @Name;
                           """;

        int c = await connection.QuerySingleAsync<int>(
            new CommandDefinition(sql, new { Name = name }, cancellationToken: cancellationToken));

        return c > 0;
    }

    private static async Task<int> DeleteLoopAsync(
        SqlConnection connection,
        string sql,
        Guid tenantId,
        int cap,
        Dictionary<string, int> counts,
        string key,
        CancellationToken cancellationToken)
    {
        int total = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            int affected = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { TenantId = tenantId, Cap = cap },
                    cancellationToken: cancellationToken));

            if (affected == 0)
                break;


            total += affected;
        }

        if (total > 0)

            counts[key] = counts.GetValueOrDefault(key, 0) + total;


        return total;
    }
}
