using System.Data;

namespace ArchLucid.Persistence.Data.Infrastructure;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();

    /// <summary>
    ///     Creates and asynchronously opens a new database connection.
    /// </summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
