using System.Diagnostics.CodeAnalysis;

using ArchiForge.Core.Conversation;
using ArchiForge.Core.Pagination;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Conversation;

/// <summary>
/// SQL Server <see cref="IConversationThreadRepository"/> for <c>dbo.ConversationThreads</c>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperConversationThreadRepository(ISqlConnectionFactory connectionFactory)
    : IConversationThreadRepository
{
    /// <inheritdoc />
    public async Task CreateAsync(ConversationThread thread, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(thread);
        const string sql = """
            INSERT INTO dbo.ConversationThreads
            (
                ThreadId, TenantId, WorkspaceId, ProjectId,
                RunId, BaseRunId, TargetRunId,
                Title, CreatedUtc, LastUpdatedUtc
            )
            VALUES
            (
                @ThreadId, @TenantId, @WorkspaceId, @ProjectId,
                @RunId, @BaseRunId, @TargetRunId,
                @Title, @CreatedUtc, @LastUpdatedUtc
            );
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, thread, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<ConversationThread?> GetByIdAsync(Guid threadId, CancellationToken ct)
    {
        const string sql = """
            SELECT ThreadId, TenantId, WorkspaceId, ProjectId,
                   RunId, BaseRunId, TargetRunId,
                   Title, CreatedUtc, LastUpdatedUtc, ArchivedUtc
            FROM dbo.ConversationThreads
            WHERE ThreadId = @ThreadId
              AND ArchivedUtc IS NULL;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<ConversationThread>(
            new CommandDefinition(sql, new
            {
                ThreadId = threadId
            }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationThread>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 200);
        const string sql = """
            SELECT TOP (@Take)
                ThreadId, TenantId, WorkspaceId, ProjectId,
                RunId, BaseRunId, TargetRunId,
                Title, CreatedUtc, LastUpdatedUtc, ArchivedUtc
            FROM dbo.ConversationThreads
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND ArchivedUtc IS NULL
            ORDER BY LastUpdatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<ConversationThread> rows = await connection.QueryAsync<ConversationThread>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Take = take
                },
                cancellationToken: ct));
        return rows.ToList();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<ConversationThread> Items, int TotalCount)> ListByScopePagedAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int skip,
        int take,
        CancellationToken ct)
    {
        take = Math.Clamp(take, 1, PaginationDefaults.MaxPageSize);
        skip = Math.Max(skip, 0);

        const string countSql = """
            SELECT COUNT(*)
            FROM dbo.ConversationThreads
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND ArchivedUtc IS NULL;
            """;

        const string pageSql = """
            SELECT
                ThreadId, TenantId, WorkspaceId, ProjectId,
                RunId, BaseRunId, TargetRunId,
                Title, CreatedUtc, LastUpdatedUtc, ArchivedUtc
            FROM dbo.ConversationThreads
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
              AND ArchivedUtc IS NULL
            ORDER BY LastUpdatedUtc DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        object parameters = new
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            Skip = skip,
            Take = take
        };

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        int total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));
        IEnumerable<ConversationThread> rows = await connection.QueryAsync<ConversationThread>(
            new CommandDefinition(pageSql, parameters, cancellationToken: ct));

        return (rows.ToList(), total);
    }

    /// <inheritdoc />
    public async Task UpdateLastUpdatedAsync(Guid threadId, DateTime updatedUtc, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.ConversationThreads
            SET LastUpdatedUtc = @UpdatedUtc
            WHERE ThreadId = @ThreadId;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                ThreadId = threadId,
                UpdatedUtc = updatedUtc
            }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> ArchiveThreadsLastUpdatedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.ConversationThreads
            SET ArchivedUtc = SYSUTCDATETIME()
            WHERE ArchivedUtc IS NULL AND LastUpdatedUtc < @Cutoff;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Cutoff = cutoffUtc.UtcDateTime }, cancellationToken: ct));
    }
}
