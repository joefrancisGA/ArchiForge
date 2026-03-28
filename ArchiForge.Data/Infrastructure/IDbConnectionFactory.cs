using System.Data;

namespace ArchiForge.Data.Infrastructure;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();

    /// <summary>
    /// Creates and asynchronously opens a new database connection.
    /// </summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// When <see langword="false"/> (SQLite), callers must not wrap multiple repository operations in
    /// <see cref="System.Transactions.TransactionScope"/>—each call opens a new connection and ambient enlistment is not supported.
    /// SQL Server returns <see langword="true"/>.
    /// </summary>
    bool SupportsAmbientTransactionScope { get; }
}
