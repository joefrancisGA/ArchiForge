using System.Diagnostics.CodeAnalysis;

using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Orchestration;

/// <summary>Dapper implementation over <c>dbo.AuthorityPipelineWorkOutbox</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository.")]
public sealed class DapperAuthorityPipelineWorkRepository(ISqlConnectionFactory connectionFactory)
    : IAuthorityPipelineWorkRepository
{
    /// <inheritdoc />
    public async Task EnqueueAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        const string sql = """
            INSERT INTO dbo.AuthorityPipelineWorkOutbox
            (OutboxId, RunId, TenantId, WorkspaceId, ProjectId, PayloadJson, CreatedUtc)
            VALUES (@OutboxId, @RunId, @TenantId, @WorkspaceId, @ProjectId, @PayloadJson, SYSUTCDATETIME());
            """;

        Guid outboxId = Guid.NewGuid();
        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    OutboxId = outboxId,
                    RunId = runId,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    PayloadJson = payloadJson,
                },
                cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuthorityPipelineWorkOutboxEntry>> DequeuePendingAsync(int maxBatch, CancellationToken cancellationToken)
    {
        int take = Math.Clamp(maxBatch, 1, 100);
        const string sql = """
            SELECT TOP (@Take)
                OutboxId, RunId, TenantId, WorkspaceId, ProjectId, PayloadJson, CreatedUtc
            FROM dbo.AuthorityPipelineWorkOutbox
            WHERE ProcessedUtc IS NULL
            ORDER BY CreatedUtc ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        IEnumerable<AuthorityPipelineWorkOutboxEntry> rows = await connection.QueryAsync<AuthorityPipelineWorkOutboxEntry>(
            new CommandDefinition(sql, new { Take = take }, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task MarkProcessedAsync(Guid outboxId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.AuthorityPipelineWorkOutbox
            SET ProcessedUtc = SYSUTCDATETIME()
            WHERE OutboxId = @OutboxId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { OutboxId = outboxId }, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<long> CountPendingAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.AuthorityPipelineWorkOutbox
            WHERE ProcessedUtc IS NULL;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        long count = await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, cancellationToken: cancellationToken));

        return count;
    }
}
