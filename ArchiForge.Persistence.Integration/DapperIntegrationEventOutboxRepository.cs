using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Integration;

/// <summary>Dapper implementation over <c>dbo.IntegrationEventOutbox</c>.</summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperIntegrationEventOutboxRepository(ISqlConnectionFactory connectionFactory)
    : IIntegrationEventOutboxRepository
{
    /// <inheritdoc />
    public async Task EnqueueAsync(
        Guid runId,
        string eventType,
        string? messageId,
        ReadOnlyMemory<byte> payloadUtf8,
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
            eventType,
            messageId,
            payloadUtf8,
            tenantId,
            workspaceId,
            projectId,
            ct);
    }

    /// <inheritdoc />
    public Task EnqueueAsync(
        Guid runId,
        string eventType,
        string? messageId,
        ReadOnlyMemory<byte> payloadUtf8,
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
            eventType,
            messageId,
            payloadUtf8,
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
        string eventType,
        string? messageId,
        ReadOnlyMemory<byte> payloadUtf8,
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        byte[] bytes = payloadUtf8.ToArray();

        const string sql = """
            INSERT INTO dbo.IntegrationEventOutbox
            (OutboxId, RunId, EventType, MessageId, PayloadUtf8, TenantId, WorkspaceId, ProjectId, CreatedUtc)
            VALUES (@OutboxId, @RunId, @EventType, @MessageId, @PayloadUtf8, @TenantId, @WorkspaceId, @ProjectId, SYSUTCDATETIME());
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    OutboxId = outboxId,
                    RunId = runId,
                    EventType = eventType,
                    MessageId = messageId,
                    PayloadUtf8 = bytes,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId
                },
                transaction: transaction,
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IntegrationEventOutboxEntry>> DequeuePendingAsync(int maxBatch, CancellationToken ct)
    {
        int take = Math.Clamp(maxBatch, 1, 100);

        const string sql = """
            SELECT TOP (@Take)
                OutboxId, RunId, EventType, MessageId, PayloadUtf8, TenantId, WorkspaceId, ProjectId, CreatedUtc,
                RetryCount, NextRetryUtc, LastErrorMessage, DeadLetteredUtc
            FROM dbo.IntegrationEventOutbox
            WHERE ProcessedUtc IS NULL
              AND DeadLetteredUtc IS NULL
              AND (NextRetryUtc IS NULL OR NextRetryUtc <= SYSUTCDATETIME())
            ORDER BY CreatedUtc ASC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        IEnumerable<IntegrationEventOutboxRow> rows = await connection.QueryAsync<IntegrationEventOutboxRow>(
            new CommandDefinition(sql, new { Take = take }, cancellationToken: ct));

        List<IntegrationEventOutboxEntry> list = [];

        foreach (IntegrationEventOutboxRow row in rows)
        {
            if (row.PayloadUtf8 is null || row.EventType is null)
            {
                continue;
            }

            list.Add(
                new IntegrationEventOutboxEntry
                {
                    OutboxId = row.OutboxId,
                    RunId = row.RunId,
                    EventType = row.EventType,
                    MessageId = row.MessageId,
                    PayloadUtf8 = row.PayloadUtf8,
                    TenantId = row.TenantId,
                    WorkspaceId = row.WorkspaceId,
                    ProjectId = row.ProjectId,
                    CreatedUtc = row.CreatedUtc,
                    RetryCount = row.RetryCount,
                    NextRetryUtc = row.NextRetryUtc,
                    LastErrorMessage = row.LastErrorMessage,
                    DeadLetteredUtc = row.DeadLetteredUtc
                });
        }

        return list;
    }

    /// <inheritdoc />
    public async Task MarkProcessedAsync(Guid outboxId, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.IntegrationEventOutbox
            SET ProcessedUtc = SYSUTCDATETIME()
            WHERE OutboxId = @OutboxId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        await connection.ExecuteAsync(new CommandDefinition(sql, new { OutboxId = outboxId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task RecordPublishFailureAsync(
        Guid outboxId,
        int newRetryCount,
        DateTime? nextRetryUtc,
        DateTime? deadLetteredUtc,
        string? lastErrorMessage,
        CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.IntegrationEventOutbox
            SET RetryCount = @NewRetryCount,
                NextRetryUtc = @NextRetryUtc,
                DeadLetteredUtc = @DeadLetteredUtc,
                LastErrorMessage = @LastErrorMessage
            WHERE OutboxId = @OutboxId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    OutboxId = outboxId,
                    NewRetryCount = newRetryCount,
                    NextRetryUtc = nextRetryUtc,
                    DeadLetteredUtc = deadLetteredUtc,
                    LastErrorMessage = TruncateError(lastErrorMessage)
                },
                cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<long> CountIntegrationOutboxPublishPendingAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.IntegrationEventOutbox
            WHERE ProcessedUtc IS NULL
              AND DeadLetteredUtc IS NULL;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        long count = await connection.QuerySingleAsync<long>(new CommandDefinition(sql, cancellationToken: ct));

        return count;
    }

    /// <inheritdoc />
    public async Task<long> CountIntegrationOutboxDeadLetterAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.IntegrationEventOutbox
            WHERE DeadLetteredUtc IS NOT NULL
              AND ProcessedUtc IS NULL;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        long count = await connection.QuerySingleAsync<long>(new CommandDefinition(sql, cancellationToken: ct));

        return count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IntegrationEventOutboxDeadLetterRow>> ListDeadLettersAsync(int maxRows, CancellationToken ct)
    {
        int take = Math.Clamp(maxRows, 1, 500);

        const string sql = """
            SELECT TOP (@Take)
                OutboxId, RunId, EventType, DeadLetteredUtc, RetryCount, LastErrorMessage
            FROM dbo.IntegrationEventOutbox
            WHERE DeadLetteredUtc IS NOT NULL
              AND ProcessedUtc IS NULL
            ORDER BY DeadLetteredUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        IEnumerable<DeadLetterRow> rows = await connection.QueryAsync<DeadLetterRow>(
            new CommandDefinition(sql, new { Take = take }, cancellationToken: ct));

        List<IntegrationEventOutboxDeadLetterRow> list = [];

        foreach (DeadLetterRow row in rows)
        {
            if (row.EventType is null)
            {
                continue;
            }

            list.Add(
                new IntegrationEventOutboxDeadLetterRow
                {
                    OutboxId = row.OutboxId,
                    RunId = row.RunId,
                    EventType = row.EventType,
                    DeadLetteredUtc = row.DeadLetteredUtc,
                    RetryCount = row.RetryCount,
                    LastErrorMessage = row.LastErrorMessage
                });
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<bool> ResetDeadLetterForRetryAsync(Guid outboxId, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.IntegrationEventOutbox
            SET DeadLetteredUtc = NULL,
                RetryCount = 0,
                NextRetryUtc = NULL,
                LastErrorMessage = NULL
            WHERE OutboxId = @OutboxId
              AND DeadLetteredUtc IS NOT NULL;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        int rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { OutboxId = outboxId }, cancellationToken: ct));

        return rows > 0;
    }

    private static string? TruncateError(string? message)
    {
        if (message is null)
        {
            return null;
        }

        const int maxLen = 2048;

        return message.Length <= maxLen ? message : message[..maxLen];
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Dapper materialization.")]
    private sealed class IntegrationEventOutboxRow
    {
        public Guid OutboxId { get; init; }

        public Guid? RunId { get; init; }

        public string? EventType { get; init; }

        public string? MessageId { get; init; }

        public byte[]? PayloadUtf8 { get; init; }

        public Guid TenantId { get; init; }

        public Guid WorkspaceId { get; init; }

        public Guid ProjectId { get; init; }

        public DateTime CreatedUtc { get; init; }

        public int RetryCount { get; init; }

        public DateTime? NextRetryUtc { get; init; }

        public string? LastErrorMessage { get; init; }

        public DateTime? DeadLetteredUtc { get; init; }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Dapper materialization.")]
    private sealed class DeadLetterRow
    {
        public Guid OutboxId { get; init; }

        public Guid? RunId { get; init; }

        public string? EventType { get; init; }

        public DateTime DeadLetteredUtc { get; init; }

        public int RetryCount { get; init; }

        public string? LastErrorMessage { get; init; }
    }
}
