using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Transactions;
using ArchLucid.Persistence.Connections;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Transactions;

/// <summary>
///     Opens a connection via <see cref="ISqlConnectionFactory" /> and begins a transaction for
///     <see cref="DapperArchLucidUnitOfWork" />.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Opens SQL connection and begins transaction; requires live SQL Server.")]
public sealed class DapperArchLucidUnitOfWorkFactory(ISqlConnectionFactory connectionFactory)
    : IArchLucidUnitOfWorkFactory
{
    /// <inheritdoc />
    public async Task<IArchLucidUnitOfWork> CreateAsync(CancellationToken ct)
    {
        SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        SqlTransaction? transaction = connection.BeginTransaction();
        return new DapperArchLucidUnitOfWork(connection, transaction);
    }
}
