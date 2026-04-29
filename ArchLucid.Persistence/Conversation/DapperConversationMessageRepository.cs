using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Conversation;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Conversation;

/// <summary>
///     SQL Server <see cref="IConversationMessageRepository" /> for <c>dbo.ConversationMessages</c>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class DapperConversationMessageRepository(ISqlConnectionFactory connectionFactory)
    : IConversationMessageRepository
{
    /// <inheritdoc />
    public async Task AddAsync(ConversationMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        const string scopeSql = """
                                 SELECT TenantId, WorkspaceId, ProjectId
                                 FROM dbo.ConversationThreads
                                 WHERE ThreadId = @ThreadId;
                                 """;

        const string sql = """
                           INSERT INTO dbo.ConversationMessages
                           (
                               MessageId, ThreadId, Role, Content, CreatedUtc, MetadataJson,
                               TenantId, WorkspaceId, ProjectId
                           )
                           VALUES
                           (
                               @MessageId, @ThreadId, @Role, @Content, @CreatedUtc, @MetadataJson,
                               @TenantId, @WorkspaceId, @ProjectId
                           );
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        ConversationThreadDenormScopeRow? scopeHdr =
            await connection.QuerySingleOrDefaultAsync<ConversationThreadDenormScopeRow>(
                new CommandDefinition(scopeSql, new { message.ThreadId }, cancellationToken: ct));

        if (scopeHdr?.TenantId is null || scopeHdr.WorkspaceId is null || scopeHdr.ProjectId is null)
            throw new InvalidOperationException(
                "dbo.ConversationThreads row for ThreadId=" + message.ThreadId
                + " lacks denormalized RLS scope; cannot persist ConversationMessages.");

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    message.MessageId,
                    message.ThreadId,
                    message.Role,
                    message.Content,
                    message.CreatedUtc,
                    message.MetadataJson,
                    TenantId = scopeHdr.TenantId!.Value,
                    WorkspaceId = scopeHdr.WorkspaceId!.Value,
                    ProjectId = scopeHdr.ProjectId!.Value
                },
                cancellationToken: ct));
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
            new CommandDefinition(sql, new { ThreadId = threadId, Take = take }, cancellationToken: ct));
        return rows.ToList();
    }
}
