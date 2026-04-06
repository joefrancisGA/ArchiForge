using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Retrieval;

/// <summary>Dapper implementation of <see cref="IRetrievalIndexingOutboxRepository"/> over <c>dbo.RetrievalIndexingOutbox</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperRetrievalIndexingOutboxRepository(ISqlConnectionFactory connectionFactory)
    : IRetrievalIndexingOutboxRepository
{
    /// <inheritdoc />
    public async Task EnqueueAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        Guid outboxId = Guid.NewGuid();
        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await EnqueueCoreAsync(
            connection,
            null,
            outboxId,
            runId,
            tenantId,
            workspaceId,
            projectId,
            ct);
    }

    /// <inheritdoc />
    public Task EnqueueAsync(
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);

        Guid outboxId = Guid.NewGuid();

        return EnqueueCoreAsync(
            connection,
            transaction,
            outboxId,
            runId,
            tenantId,
            workspaceId,
            projectId,
            ct);
    }

    private static async Task EnqueueCoreAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Guid outboxId,
        Guid runId,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.RetrievalIndexingOutbox
            (OutboxId, RunId, TenantId, WorkspaceId, ProjectId, CreatedUtc)
            VALUES (@OutboxId, @RunId, @TenantId, @WorkspaceId, @ProjectId, SYSUTCDATETIME());
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    OutboxId = outboxId,
                    RunId = runId,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId
                },
                transaction: transaction,
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetrievalIndexingOutboxEntry>> DequeuePendingAsync(int maxBatch, CancellationToken ct)
    {
        int take = Math.Clamp(maxBatch, 1, 100);
        const string sql = """
            SELECT TOP (@Take)
                OutboxId, RunId, TenantId, WorkspaceId, ProjectId, CreatedUtc
            FROM dbo.RetrievalIndexingOutbox
            WHERE ProcessedUtc IS NULL
            ORDER BY CreatedUtc ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<RetrievalIndexingOutboxEntry> rows = await connection.QueryAsync<RetrievalIndexingOutboxEntry>(
            new CommandDefinition(sql, new { Take = take }, cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task MarkProcessedAsync(Guid outboxId, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.RetrievalIndexingOutbox
            SET ProcessedUtc = SYSUTCDATETIME()
            WHERE OutboxId = @OutboxId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { OutboxId = outboxId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<long> CountPendingAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.RetrievalIndexingOutbox
            WHERE ProcessedUtc IS NULL;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        long count = await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, cancellationToken: ct));

        return count;
    }
}
