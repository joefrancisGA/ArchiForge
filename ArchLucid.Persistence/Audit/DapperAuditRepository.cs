using System.Diagnostics.CodeAnalysis;

using ArchiForge.Core.Audit;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Audit;

/// <summary>
/// SQL Server-backed implementation of <see cref="IAuditRepository"/>.
/// Appends <see cref="AuditEvent"/> rows to <c>dbo.AuditEvents</c> and retrieves them
/// scoped to tenant/workspace/project with a configurable paged cap.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperAuditRepository(ISqlConnectionFactory connectionFactory) : IAuditRepository
{
    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<AuditEvent> rows = await connection.QueryAsync<AuditEvent>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Take = Math.Clamp(take <= 0 ? 100 : take, 1, 500)
                },
                cancellationToken: ct));

        return rows.ToList();
    }
}
