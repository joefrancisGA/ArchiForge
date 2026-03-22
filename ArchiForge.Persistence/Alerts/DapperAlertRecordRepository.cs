using ArchiForge.Decisioning.Alerts;
using ArchiForge.Persistence.Connections;
using Dapper;

namespace ArchiForge.Persistence.Alerts;

public sealed class DapperAlertRecordRepository(ISqlConnectionFactory connectionFactory) : IAlertRecordRepository
{
    public async Task CreateAsync(AlertRecord alert, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.AlertRecords
            (
                AlertId, RuleId, TenantId, WorkspaceId, ProjectId,
                RunId, ComparedToRunId, RecommendationId,
                Title, Category, Severity, Status,
                TriggerValue, Description, CreatedUtc, LastUpdatedUtc,
                AcknowledgedByUserId, AcknowledgedByUserName, ResolutionComment,
                DeduplicationKey
            )
            VALUES
            (
                @AlertId, @RuleId, @TenantId, @WorkspaceId, @ProjectId,
                @RunId, @ComparedToRunId, @RecommendationId,
                @Title, @Category, @Severity, @Status,
                @TriggerValue, @Description, @CreatedUtc, @LastUpdatedUtc,
                @AcknowledgedByUserId, @AcknowledgedByUserName, @ResolutionComment,
                @DeduplicationKey
            );
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, alert, cancellationToken: ct));
    }

    public async Task UpdateAsync(AlertRecord alert, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.AlertRecords
            SET
                Status = @Status,
                LastUpdatedUtc = @LastUpdatedUtc,
                AcknowledgedByUserId = @AcknowledgedByUserId,
                AcknowledgedByUserName = @AcknowledgedByUserName,
                ResolutionComment = @ResolutionComment
            WHERE AlertId = @AlertId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, alert, cancellationToken: ct));
    }

    public async Task<AlertRecord?> GetByIdAsync(Guid alertId, CancellationToken ct)
    {
        const string sql = """
            SELECT *
            FROM dbo.AlertRecords
            WHERE AlertId = @AlertId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<AlertRecord>(
            new CommandDefinition(sql, new { AlertId = alertId }, cancellationToken: ct));
    }

    public async Task<AlertRecord?> GetOpenByDeduplicationKeyAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string deduplicationKey,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1 *
            FROM dbo.AlertRecords
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND DeduplicationKey = @DeduplicationKey
              AND Status IN ('Open', 'Acknowledged')
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<AlertRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    DeduplicationKey = deduplicationKey,
                },
                cancellationToken: ct));
    }

    public async Task<IReadOnlyList<AlertRecord>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string? status,
        int take,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (@Take) *
            FROM dbo.AlertRecords
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND (@Status IS NULL OR Status = @Status)
            ORDER BY CreatedUtc DESC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<AlertRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Status = status,
                    Take = take,
                },
                cancellationToken: ct));
        return rows.ToList();
    }
}
