using System.Data;

using ArchLucid.Core.Transactions;

namespace ArchLucid.Persistence.Transactions;

/// <summary>
/// No-op unit of work for in-memory repositories: <see cref="CommitAsync"/> and <see cref="RollbackAsync"/> complete immediately; <see cref="Connection"/> and <see cref="Transaction"/> throw <see cref="NotSupportedException"/>.
/// </summary>
public sealed class InMemoryArchLucidUnitOfWork : IArchLucidUnitOfWork
{
    /// <inheritdoc />
    public bool SupportsExternalTransaction => false;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown; in-memory repositories do not use ADO.NET.</exception>
    public IDbConnection Connection =>
        throw new NotSupportedException("In-memory unit of work does not expose a SQL connection.");

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown; in-memory repositories do not use ADO.NET.</exception>
    public IDbTransaction Transaction =>
        throw new NotSupportedException("In-memory unit of work does not expose a SQL transaction.");

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken ct)
    {
        _ = ct;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RollbackAsync(CancellationToken ct)
    {
        _ = ct;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
