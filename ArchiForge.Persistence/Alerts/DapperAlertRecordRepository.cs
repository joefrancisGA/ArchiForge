using ArchiForge.Decisioning.Alerts;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Alerts;

/// <summary>
/// Dapper-backed <see cref="IAlertRecordRepository"/> against <c>dbo.AlertRecords</c>.
/// </summary>
/// <param name="connectionFactory">Opened per call; callers rely on scoped factory in DI.</param>
/// <remarks>
/// <see cref="GetOpenByDeduplicationKeyAsync"/> matches SQL status filter <c>Open</c>/<c>Acknowledged</c> (see <see cref="AlertStatus"/> constants).
/// </remarks>
public sealed class DapperAlertRecordRepository(ISqlConnectionFactory connectionFactory) : IAlertRecordRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(AlertRecord alert, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(alert);

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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, alert, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AlertRecord alert, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(alert);

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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, alert, cancellationToken: ct));
    }

    public async Task<AlertRecord?> GetByIdAsync(Guid alertId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                AlertId, RuleId, TenantId, WorkspaceId, ProjectId,
                RunId, ComparedToRunId, RecommendationId,
                Title, Category, Severity, Status,
                TriggerValue, Description, CreatedUtc, LastUpdatedUtc,
                AcknowledgedByUserId, AcknowledgedByUserName, ResolutionComment,
                DeduplicationKey
            FROM dbo.AlertRecords
            WHERE AlertId = @AlertId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<AlertRecord>(
            new CommandDefinition(sql, new
            {
                AlertId = alertId
            }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<AlertRecord?> GetOpenByDeduplicationKeyAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string deduplicationKey,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1
                AlertId, RuleId, TenantId, WorkspaceId, ProjectId,
                RunId, ComparedToRunId, RecommendationId,
                Title, Category, Severity, Status,
                TriggerValue, Description, CreatedUtc, LastUpdatedUtc,
                AcknowledgedByUserId, AcknowledgedByUserName, ResolutionComment,
                DeduplicationKey
            FROM dbo.AlertRecords
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND DeduplicationKey = @DeduplicationKey
              AND Status IN ('Open', 'Acknowledged')
            ORDER BY CreatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertRecord>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string? status,
        int take,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (@Take)
                AlertId, RuleId, TenantId, WorkspaceId, ProjectId,
                RunId, ComparedToRunId, RecommendationId,
                Title, Category, Severity, Status,
                TriggerValue, Description, CreatedUtc, LastUpdatedUtc,
                AcknowledgedByUserId, AcknowledgedByUserName, ResolutionComment,
                DeduplicationKey
            FROM dbo.AlertRecords
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND (@Status IS NULL OR Status = @Status)
            ORDER BY CreatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<AlertRecord> rows = await connection.QueryAsync<AlertRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Status = status,
                    Take = Math.Clamp(take, 1, 500),
                },
                cancellationToken: ct));
        return rows.ToList();
    }
}
