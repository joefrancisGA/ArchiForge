namespace ArchLucid.Core.Transactions;

/// <summary>
///     Creates <see cref="IArchLucidUnitOfWork" /> instances for a single orchestrated persistence flow (Dapper with open
///     connection + transaction, or in-memory no-op).
/// </summary>
public interface IArchLucidUnitOfWorkFactory
{
    /// <summary>Creates and returns a new unit of work, ready for repository operations.</summary>
    /// <param name="ct">Cancellation token passed to connection open when using Dapper.</param>
    /// <returns>An open <see cref="IArchLucidUnitOfWork" /> that must be committed or disposed by the caller.</returns>
    Task<IArchLucidUnitOfWork> CreateAsync(CancellationToken ct);
}
