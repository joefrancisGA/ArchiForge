using System.Diagnostics.CodeAnalysis;

using ArchiForge.Decisioning.Alerts;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Alerts;

/// <summary>Dapper implementation of <see cref="IAlertRuleRepository"/> over <c>dbo.AlertRules</c>.</summary>
/// <param name="connectionFactory">SQL connection factory (scoped in DI).</param>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperAlertRuleRepository(ISqlConnectionFactory connectionFactory) : IAlertRuleRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(AlertRule rule, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rule);
        const string sql = """
            INSERT INTO dbo.AlertRules
            (
                RuleId, TenantId, WorkspaceId, ProjectId,
                Name, RuleType, Severity, ThresholdValue, IsEnabled,
                TargetChannelType, MetadataJson, CreatedUtc
            )
            VALUES
            (
                @RuleId, @TenantId, @WorkspaceId, @ProjectId,
                @Name, @RuleType, @Severity, @ThresholdValue, @IsEnabled,
                @TargetChannelType, @MetadataJson, @CreatedUtc
            );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, rule, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AlertRule rule, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rule);
        const string sql = """
            UPDATE dbo.AlertRules
            SET
                Name = @Name,
                RuleType = @RuleType,
                Severity = @Severity,
                ThresholdValue = @ThresholdValue,
                IsEnabled = @IsEnabled,
                TargetChannelType = @TargetChannelType,
                MetadataJson = @MetadataJson
            WHERE RuleId = @RuleId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, rule, cancellationToken: ct));
    }

    public async Task<AlertRule?> GetByIdAsync(Guid ruleId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                RuleId, TenantId, WorkspaceId, ProjectId,
                Name, RuleType, Severity, ThresholdValue, IsEnabled,
                TargetChannelType, MetadataJson, CreatedUtc
            FROM dbo.AlertRules
            WHERE RuleId = @RuleId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<AlertRule>(
            new CommandDefinition(sql, new
            {
                RuleId = ruleId
            }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertRule>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 200
                RuleId, TenantId, WorkspaceId, ProjectId,
                Name, RuleType, Severity, ThresholdValue, IsEnabled,
                TargetChannelType, MetadataJson, CreatedUtc
            FROM dbo.AlertRules
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<AlertRule> rows = await connection.QueryAsync<AlertRule>(
            new CommandDefinition(sql, new
            {
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId
            }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertRule>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 200
                RuleId, TenantId, WorkspaceId, ProjectId,
                Name, RuleType, Severity, ThresholdValue, IsEnabled,
                TargetChannelType, MetadataJson, CreatedUtc
            FROM dbo.AlertRules
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND IsEnabled = 1
            ORDER BY CreatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<AlertRule> rows = await connection.QueryAsync<AlertRule>(
            new CommandDefinition(sql, new
            {
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId
            }, cancellationToken: ct));
        return rows.ToList();
    }
}
