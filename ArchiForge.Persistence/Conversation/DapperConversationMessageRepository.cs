using ArchiForge.Core.Conversation;
using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Conversation;

/// <summary>
/// SQL Server <see cref="IConversationMessageRepository"/> for <c>dbo.ConversationMessages</c>.
/// </summary>
public sealed class DapperConversationMessageRepository(ISqlConnectionFactory connectionFactory)
    : IConversationMessageRepository
{
    /// <inheritdoc />
    public async Task AddAsync(ConversationMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, message, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationMessage>> GetByThreadIdAsync(
        Guid threadId,
        int take,
        CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 500);
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

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        IEnumerable<ConversationMessage> rows = await connection.QueryAsync<ConversationMessage>(
            new CommandDefinition(sql, new
            {
                ThreadId = threadId,
                Take = take
            }, cancellationToken: ct));
        return rows.ToList();
    }
}
