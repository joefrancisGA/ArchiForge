using System.Data;

namespace ArchLucid.Core.Transactions;

/// <summary>
/// Connection and transaction scope for repository writes within one logical commit (orchestrators and Dapper repositories).
/// </summary>
/// <remarks>
/// <para>Dapper-backed implementations wrap a real <see cref="IDbConnection"/> and <see cref="IDbTransaction"/>.</para>
/// <para>In-memory implementations are no-ops for SQL; <see cref="Connection"/> and <see cref="Transaction"/> may throw <see cref="NotSupportedException"/>.</para>
/// <para>When <see cref="SupportsExternalTransaction"/> is <see langword="false"/>, orchestrators still call <see cref="CommitAsync"/> / <see cref="RollbackAsync"/> but repositories persist without a shared SQL transaction.</para>
/// </remarks>
public interface IArchLucidUnitOfWork : IAsyncDisposable
{
    /// <summary>When <see langword="false"/>, callers must use repository defaults (in-memory mode); when <see langword="true"/>, repositories may use <see cref="Connection"/> and <see cref="Transaction"/>.</summary>
    bool SupportsExternalTransaction { get; }

    /// <summary>Active database connection for Dapper; not supported on in-memory unit of work.</summary>
    IDbConnection Connection { get; }

    /// <summary>Ambient transaction for Dapper (begun by the factory); not supported on in-memory unit of work.</summary>
    IDbTransaction Transaction { get; }

    /// <summary>Commits the transaction. Throws if the unit of work was already completed (Dapper).</summary>
    /// <param name="ct">Cancellation token (unused for sync ADO.NET commit; reserved for future use).</param>
    Task CommitAsync(CancellationToken ct);

    /// <summary>Rolls back the transaction if not yet completed (no-op if already completed).</summary>
    /// <param name="ct">Cancellation token (unused for sync ADO.NET rollback; reserved for future use).</param>
    Task RollbackAsync(CancellationToken ct);
}
