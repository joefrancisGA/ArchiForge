using ArchiForge.Persistence.Connections;

namespace ArchiForge.Persistence.Transactions;

public sealed class DapperArchiForgeUnitOfWorkFactory : IArchiForgeUnitOfWorkFactory
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public DapperArchiForgeUnitOfWorkFactory(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IArchiForgeUnitOfWork> CreateAsync(CancellationToken ct)
    {
        var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var transaction = connection.BeginTransaction();
        return new DapperArchiForgeUnitOfWork(connection, transaction);
    }
}
