using ArchiForge.Core.Conversation;
using ArchiForge.Persistence.Connections;
using Dapper;

namespace ArchiForge.Persistence.Conversation;

public sealed class DapperConversationMessageRepository(ISqlConnectionFactory connectionFactory)
    : IConversationMessageRepository
{
    public async Task AddAsync(ConversationMessage message, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.ConversationMessages
            (
                MessageId, ThreadId, Role, Content, CreatedUtc, MetadataJson
            )
            VALUES
            (
                @MessageId, @ThreadId, @Role, @Content, @CreatedUtc, @MetadataJson
            );
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, message, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ConversationMessage>> GetByThreadIdAsync(
        Guid threadId,
        int take,
        CancellationToken ct)
    {
        const string sql = """
            SELECT MessageId, ThreadId, Role, Content, CreatedUtc, MetadataJson
            FROM (
                SELECT TOP (@Take)
                    MessageId, ThreadId, Role, Content, CreatedUtc, MetadataJson
                FROM dbo.ConversationMessages
                WHERE ThreadId = @ThreadId
                ORDER BY CreatedUtc DESC
            ) AS recent
            ORDER BY CreatedUtc ASC;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<ConversationMessage>(
            new CommandDefinition(sql, new { ThreadId = threadId, Take = take }, cancellationToken: ct));
        return rows.ToList();
    }
}
