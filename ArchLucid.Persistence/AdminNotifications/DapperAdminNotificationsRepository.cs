using ArchLucid.Core.AdminNotifications;

using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.AdminNotifications;

public sealed class DapperAdminNotificationsRepository(ISqlConnectionFactory connectionFactory) : IAdminNotificationsRepository
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <inheritdoc />
    public async Task InsertAsync(string kind, string summary, string? dataJson, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string sql = """
                           INSERT INTO dbo.AdminNotifications (Kind, Summary, DataJson)
                           VALUES (@Kind, @Summary, @DataJson);
                           """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Kind = kind, Summary = summary, DataJson = dataJson },
                cancellationToken: cancellationToken));
    }
}
