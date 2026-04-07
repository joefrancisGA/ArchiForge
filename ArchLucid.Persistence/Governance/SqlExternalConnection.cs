using System.Data;

using ArchLucid.Persistence.Connections;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Governance;

/// <summary>
/// Resolves a SQL connection for policy-pack writes: reuse UoW connection or open a dedicated <see cref="SqlConnection"/>.
/// </summary>
internal static class SqlExternalConnection
{
    public static async Task<(SqlConnection Connection, bool OwnsConnection)> ResolveAsync(
        ISqlConnectionFactory connectionFactory,
        IDbConnection? connection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        if (connection is SqlConnection sqlConnection)
            return (sqlConnection, false);

        if (connection is not null)
            throw new ArgumentException("Policy pack SQL repositories require a SqlConnection when an external connection is supplied.", nameof(connection));

        SqlConnection opened = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return (opened, true);
    }

    public static void DisposeIfOwned(SqlConnection connection, bool ownsConnection)
    {
        if (ownsConnection)
            connection.Dispose();
    }
}
