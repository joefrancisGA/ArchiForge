using System.Data;

namespace ArchiForge.Persistence.Transactions;

public interface IArchiForgeUnitOfWork : IAsyncDisposable
{
    /// <summary>When false, callers must persist using repository defaults (e.g. in-memory mode).</summary>
    bool SupportsExternalTransaction { get; }

    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }

    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
