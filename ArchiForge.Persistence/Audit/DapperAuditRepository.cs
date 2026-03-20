using ArchiForge.Core.Audit;
using ArchiForge.Persistence.Connections;
using Dapper;

namespace ArchiForge.Persistence.Audit;

public sealed class DapperAuditRepository : IAuditRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public DapperAuditRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.AuditEvents (
                EventId, OccurredUtc, EventType,
                ActorUserId, ActorUserName,
                TenantId, WorkspaceId, ProjectId,
                RunId, ManifestId, ArtifactId,
                DataJson, CorrelationId
            )
            VALUES (
                @EventId, @OccurredUtc, @EventType,
                @ActorUserId, @ActorUserName,
                @TenantId, @WorkspaceId, @ProjectId,
                @RunId, @ManifestId, @ArtifactId,
                @DataJson, @CorrelationId
            );
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, auditEvent, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<AuditEvent>> GetByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (@Take)
                EventId, OccurredUtc, EventType,
                ActorUserId, ActorUserName,
                TenantId, WorkspaceId, ProjectId,
                RunId, ManifestId, ArtifactId,
                DataJson, CorrelationId
            FROM dbo.AuditEvents
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
            ORDER BY OccurredUtc DESC;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<AuditEvent>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Take = take <= 0 ? 100 : take
                },
                cancellationToken: ct));

        return rows.ToList();
    }
}
