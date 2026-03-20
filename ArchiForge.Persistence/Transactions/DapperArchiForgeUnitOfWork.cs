using System.Data;

namespace ArchiForge.Persistence.Transactions;

public sealed class DapperArchiForgeUnitOfWork : IArchiForgeUnitOfWork
{
    private bool _completed;

    public DapperArchiForgeUnitOfWork(IDbConnection connection, IDbTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    public bool SupportsExternalTransaction => true;

    public IDbConnection Connection { get; }
    public IDbTransaction Transaction { get; }

    public Task CommitAsync(CancellationToken ct)
    {
        _ = ct;
        if (_completed)
            throw new InvalidOperationException("Unit of work has already been completed.");

        Transaction.Commit();
        _completed = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct)
    {
        _ = ct;
        if (_completed)
            return Task.CompletedTask;

        Transaction.Rollback();
        _completed = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            try
            {
                Transaction.Rollback();
            }
            catch
            {
                // best-effort
            }

            _completed = true;
        }

        Transaction.Dispose();
        Connection.Dispose();
        return ValueTask.CompletedTask;
    }
}
