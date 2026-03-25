using ArchiForge.Core.Conversation;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Conversation;

/// <summary>
/// SQL Server <see cref="IConversationThreadRepository"/> for <c>dbo.ConversationThreads</c>.
/// </summary>
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
                   Title, CreatedUtc, LastUpdatedUtc
            FROM dbo.ConversationThreads
            WHERE ThreadId = @ThreadId;
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
                Title, CreatedUtc, LastUpdatedUtc
            FROM dbo.ConversationThreads
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ProjectId
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
}
