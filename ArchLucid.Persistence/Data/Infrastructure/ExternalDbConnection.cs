using System.Data;

namespace ArchLucid.Persistence.Data.Infrastructure;

/// <summary>
/// Resolves an open <see cref="IDbConnection"/> for Dapper: reuse a caller-supplied connection (unit of work) or open and own a new one.
/// </summary>
internal static class ExternalDbConnection
{
    public static async Task<(IDbConnection Connection, bool OwnsConnection)> ResolveAsync(
        IDbConnectionFactory connectionFactory,
        IDbConnection? connection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        if (connection is not null)
            return (connection, false);

        IDbConnection opened = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return (opened, true);
    }

    public static void DisposeIfOwned(IDbConnection connection, bool ownsConnection)
    {
        if (ownsConnection)
            connection.Dispose();
    }
}
