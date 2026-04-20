using System.Diagnostics.CodeAnalysis;
using System.Text;

using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Audit;

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
        // Read-committed + row-versioning (RCSI): consistent committed reads without dirty-read hints; enable via migration 091.
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

    public async Task<IReadOnlyList<AuditEvent>> GetFilteredAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        AuditEventFilter filter,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filter);

        int take = Math.Clamp(filter.Take <= 0 ? 100 : filter.Take, 1, 500);

        // RCSI-backed read committed: no dirty reads on audit listing (see migration 091).
        StringBuilder sql = new("""
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
            """);

        DynamicParameters parameters = new();
        parameters.Add("TenantId", tenantId);
        parameters.Add("WorkspaceId", workspaceId);
        parameters.Add("ProjectId", projectId);
        parameters.Add("Take", take);

        if (!string.IsNullOrWhiteSpace(filter.EventType))
        {
            sql.Append(" AND EventType = @EventType");
            parameters.Add("EventType", filter.EventType);
        }

        if (filter.FromUtc.HasValue)
        {
            sql.Append(" AND OccurredUtc >= @FromUtc");
            parameters.Add("FromUtc", filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            sql.Append(" AND OccurredUtc <= @ToUtc");
            parameters.Add("ToUtc", filter.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            sql.Append(" AND CorrelationId = @CorrelationId");
            parameters.Add("CorrelationId", filter.CorrelationId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActorUserId))
        {
            sql.Append(" AND ActorUserId = @ActorUserId");
            parameters.Add("ActorUserId", filter.ActorUserId);
        }

        if (filter.RunId.HasValue)
        {
            sql.Append(" AND RunId = @RunId");
            parameters.Add("RunId", filter.RunId.Value);
        }

        if (filter.BeforeUtc.HasValue)
        {
            sql.Append(" AND OccurredUtc < @BeforeUtc");
            parameters.Add("BeforeUtc", filter.BeforeUtc.Value);
        }

        sql.Append(" ORDER BY OccurredUtc DESC;");

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<AuditEvent> rows = await connection.QueryAsync<AuditEvent>(
            new CommandDefinition(sql.ToString(), parameters, cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<AuditEvent>> GetExportAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime fromUtc,
        DateTime toUtc,
        int maxRows,
        CancellationToken ct)
    {
        int take = Math.Clamp(maxRows <= 0 ? 10_000 : maxRows, 1, 10_000);

        // Export uses the same committed-read semantics as list/filter (RCSI when enabled).
        const string sql = """
            SELECT TOP (@MaxRows)
                EventId, OccurredUtc, EventType,
                ActorUserId, ActorUserName,
                TenantId, WorkspaceId, ProjectId,
                RunId, ManifestId, ArtifactId,
                DataJson, CorrelationId
            FROM dbo.AuditEvents
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND OccurredUtc >= @FromUtc
              AND OccurredUtc < @ToUtc
            ORDER BY OccurredUtc ASC;
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
                    FromUtc = fromUtc,
                    ToUtc = toUtc,
                    MaxRows = take,
                },
                cancellationToken: ct));

        return rows.ToList();
    }
}
