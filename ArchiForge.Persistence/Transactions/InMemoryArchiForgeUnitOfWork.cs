using System.Data;

namespace ArchiForge.Persistence.Transactions;

public sealed class InMemoryArchiForgeUnitOfWork : IArchiForgeUnitOfWork
{
    public bool SupportsExternalTransaction => false;

    public IDbConnection Connection =>
        throw new NotSupportedException("In-memory unit of work does not expose a SQL connection.");

    public IDbTransaction Transaction =>
        throw new NotSupportedException("In-memory unit of work does not expose a SQL transaction.");

    public Task CommitAsync(CancellationToken ct)
    {
        _ = ct;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct)
    {
        _ = ct;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
