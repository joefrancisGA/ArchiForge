using ArchiForge.Core.Conversation;
using ArchiForge.Persistence.Connections;
using Dapper;

namespace ArchiForge.Persistence.Conversation;

public sealed class DapperConversationThreadRepository(ISqlConnectionFactory connectionFactory)
    : IConversationThreadRepository
{
    public async Task CreateAsync(ConversationThread thread, CancellationToken ct)
    {
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

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, thread, cancellationToken: ct));
    }

    public async Task<ConversationThread?> GetByIdAsync(Guid threadId, CancellationToken ct)
    {
        const string sql = """
            SELECT ThreadId, TenantId, WorkspaceId, ProjectId,
                   RunId, BaseRunId, TargetRunId,
                   Title, CreatedUtc, LastUpdatedUtc
            FROM dbo.ConversationThreads
            WHERE ThreadId = @ThreadId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<ConversationThread>(
            new CommandDefinition(sql, new { ThreadId = threadId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ConversationThread>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken ct)
    {
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

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<ConversationThread>(
            new CommandDefinition(
                sql,
                new { TenantId = tenantId, WorkspaceId = workspaceId, ProjectId = projectId, Take = take },
                cancellationToken: ct));
        return rows.ToList();
    }

    public async Task UpdateLastUpdatedAsync(Guid threadId, DateTime updatedUtc, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.ConversationThreads
            SET LastUpdatedUtc = @UpdatedUtc
            WHERE ThreadId = @ThreadId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ThreadId = threadId, UpdatedUtc = updatedUtc }, cancellationToken: ct));
    }
}
