using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Persistence.Connections;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Alerts;

public sealed class DapperCompositeAlertRuleRepository(ISqlConnectionFactory connectionFactory)
    : ICompositeAlertRuleRepository
{
    public async Task CreateAsync(CompositeAlertRule rule, CancellationToken ct)
    {
        const string insertRule = """
            INSERT INTO dbo.CompositeAlertRules
            (
                CompositeRuleId, TenantId, WorkspaceId, ProjectId,
                Name, Severity, [Operator], IsEnabled,
                SuppressionWindowMinutes, CooldownMinutes, ReopenDeltaThreshold,
                DedupeScope, TargetChannelType, CreatedUtc
            )
            VALUES
            (
                @CompositeRuleId, @TenantId, @WorkspaceId, @ProjectId,
                @Name, @Severity, @Operator, @IsEnabled,
                @SuppressionWindowMinutes, @CooldownMinutes, @ReopenDeltaThreshold,
                @DedupeScope, @TargetChannelType, @CreatedUtc
            );
            """;

        const string insertCondition = """
            INSERT INTO dbo.CompositeAlertRuleConditions
            (ConditionId, CompositeRuleId, MetricType, [Operator], ThresholdValue)
            VALUES (@ConditionId, @CompositeRuleId, @MetricType, @Operator, @ThresholdValue);
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(insertRule, rule, transaction: tx, cancellationToken: ct));

            foreach (var c in rule.Conditions)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertCondition,
                        new
                        {
                            c.ConditionId,
                            CompositeRuleId = rule.CompositeRuleId,
                            c.MetricType,
                            c.Operator,
                            c.ThresholdValue,
                        },
                        transaction: tx,
                        cancellationToken: ct));
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task UpdateAsync(CompositeAlertRule rule, CancellationToken ct)
    {
        const string updateRule = """
            UPDATE dbo.CompositeAlertRules
            SET
                Name = @Name,
                Severity = @Severity,
                [Operator] = @Operator,
                IsEnabled = @IsEnabled,
                SuppressionWindowMinutes = @SuppressionWindowMinutes,
                CooldownMinutes = @CooldownMinutes,
                ReopenDeltaThreshold = @ReopenDeltaThreshold,
                DedupeScope = @DedupeScope,
                TargetChannelType = @TargetChannelType
            WHERE CompositeRuleId = @CompositeRuleId;
            """;

        const string deleteConditions = """
            DELETE FROM dbo.CompositeAlertRuleConditions
            WHERE CompositeRuleId = @CompositeRuleId;
            """;

        const string insertCondition = """
            INSERT INTO dbo.CompositeAlertRuleConditions
            (ConditionId, CompositeRuleId, MetricType, [Operator], ThresholdValue)
            VALUES (@ConditionId, @CompositeRuleId, @MetricType, @Operator, @ThresholdValue);
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(updateRule, rule, transaction: tx, cancellationToken: ct));
            await connection.ExecuteAsync(
                new CommandDefinition(
                    deleteConditions,
                    new { rule.CompositeRuleId },
                    transaction: tx,
                    cancellationToken: ct));

            foreach (var c in rule.Conditions)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertCondition,
                        new
                        {
                            c.ConditionId,
                            CompositeRuleId = rule.CompositeRuleId,
                            c.MetricType,
                            c.Operator,
                            c.ThresholdValue,
                        },
                        transaction: tx,
                        cancellationToken: ct));
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<CompositeAlertRule?> GetByIdAsync(Guid compositeRuleId, CancellationToken ct)
    {
        const string sqlRule = """
            SELECT
                CompositeRuleId, TenantId, WorkspaceId, ProjectId,
                Name, Severity, [Operator] AS Operator, IsEnabled,
                SuppressionWindowMinutes, CooldownMinutes, ReopenDeltaThreshold,
                DedupeScope, TargetChannelType, CreatedUtc
            FROM dbo.CompositeAlertRules
            WHERE CompositeRuleId = @CompositeRuleId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rule = await connection.QueryFirstOrDefaultAsync<CompositeAlertRule>(
            new CommandDefinition(sqlRule, new { CompositeRuleId = compositeRuleId }, cancellationToken: ct));

        if (rule is null)
            return null;

        await HydrateConditionsAsync(connection, [rule], ct).ConfigureAwait(false);
        return rule;
    }

    public async Task<IReadOnlyList<CompositeAlertRule>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                CompositeRuleId, TenantId, WorkspaceId, ProjectId,
                Name, Severity, [Operator] AS Operator, IsEnabled,
                SuppressionWindowMinutes, CooldownMinutes, ReopenDeltaThreshold,
                DedupeScope, TargetChannelType, CreatedUtc
            FROM dbo.CompositeAlertRules
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rules = (await connection.QueryAsync<CompositeAlertRule>(
                new CommandDefinition(
                    sql,
                    new { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId },
                    cancellationToken: ct)))
            .ToList();

        await HydrateConditionsAsync(connection, rules, ct).ConfigureAwait(false);
        return rules;
    }

    public Task<IReadOnlyList<CompositeAlertRule>> ListEnabledByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct) =>
        ListByScopeFilteredAsync(tenantId, workspaceId, projectId, enabledOnly: true, ct);

    private async Task<IReadOnlyList<CompositeAlertRule>> ListByScopeFilteredAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        bool enabledOnly,
        CancellationToken ct)
    {
        var sql = """
            SELECT
                CompositeRuleId, TenantId, WorkspaceId, ProjectId,
                Name, Severity, [Operator] AS Operator, IsEnabled,
                SuppressionWindowMinutes, CooldownMinutes, ReopenDeltaThreshold,
                DedupeScope, TargetChannelType, CreatedUtc
            FROM dbo.CompositeAlertRules
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            """;

        if (enabledOnly)
            sql += " AND IsEnabled = 1";

        sql += " ORDER BY CreatedUtc DESC;";

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rules = (await connection.QueryAsync<CompositeAlertRule>(
                new CommandDefinition(
                    sql,
                    new { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId },
                    cancellationToken: ct)))
            .ToList();

        await HydrateConditionsAsync(connection, rules, ct).ConfigureAwait(false);
        return rules;
    }

    private static async Task HydrateConditionsAsync(
        SqlConnection connection,
        IReadOnlyList<CompositeAlertRule> rules,
        CancellationToken ct)
    {
        if (rules.Count == 0)
            return;

        const string sql = """
            SELECT
                ConditionId, CompositeRuleId, MetricType, [Operator] AS Operator, ThresholdValue
            FROM dbo.CompositeAlertRuleConditions
            WHERE CompositeRuleId IN @Ids;
            """;

        var ids = rules.Select(r => r.CompositeRuleId).ToArray();
        var rows = await connection.QueryAsync<ConditionRow>(
            new CommandDefinition(sql, new { Ids = ids }, cancellationToken: ct));

        var byRule = rows.GroupBy(x => x.CompositeRuleId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var rule in rules)
        {
            rule.Conditions.Clear();
            if (!byRule.TryGetValue(rule.CompositeRuleId, out var list))
                continue;

            foreach (var row in list)
            {
                rule.Conditions.Add(
                    new AlertRuleCondition
                    {
                        ConditionId = row.ConditionId,
                        MetricType = row.MetricType,
                        Operator = row.Operator,
                        ThresholdValue = row.ThresholdValue,
                    });
            }
        }
    }

    private sealed class ConditionRow
    {
        public Guid ConditionId { get; set; }
        public Guid CompositeRuleId { get; set; }
        public string MetricType { get; set; } = null!;
        public string Operator { get; set; } = null!;
        public decimal ThresholdValue { get; set; }
    }
}
