using ArchiForge.Persistence.Connections;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Transactions;

/// <summary>
/// Opens a connection via <see cref="ISqlConnectionFactory"/> and begins a transaction for <see cref="DapperArchiForgeUnitOfWork"/>.
/// </summary>
public sealed class DapperArchiForgeUnitOfWorkFactory(ISqlConnectionFactory connectionFactory)
    : IArchiForgeUnitOfWorkFactory
{
    /// <inheritdoc />
    public async Task<IArchiForgeUnitOfWork> CreateAsync(CancellationToken ct)
    {
        SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        SqlTransaction? transaction = connection.BeginTransaction();
        return new DapperArchiForgeUnitOfWork(connection, transaction);
    }
}
