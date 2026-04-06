using System.Data;

namespace ArchiForge.Persistence.Transactions;

/// <summary>
/// Connection and transaction scope for repository writes within one logical commit (used by <see cref="ArchiForge.Persistence.Orchestration.IAuthorityRunOrchestrator"/> and Dapper repositories).
/// </summary>
/// <remarks>
/// <para><see cref="DapperArchiForgeUnitOfWork"/> wraps a real <see cref="IDbConnection"/> and <see cref="IDbTransaction"/> opened by <see cref="DapperArchiForgeUnitOfWorkFactory"/>.</para>
/// <para><see cref="InMemoryArchiForgeUnitOfWork"/> is a no-op for SQL; <see cref="Connection"/> and <see cref="Transaction"/> throw <see cref="NotSupportedException"/>.</para>
/// <para>When <see cref="SupportsExternalTransaction"/> is <see langword="false"/>, orchestrators still call <see cref="CommitAsync"/> / <see cref="RollbackAsync"/> but repositories persist via in-memory stores without a shared SQL transaction.</para>
/// </remarks>
public interface IArchiForgeUnitOfWork : IAsyncDisposable
{
    /// <summary>When <see langword="false"/>, callers must use repository defaults (in-memory mode); when <see langword="true"/>, repositories may use <see cref="Connection"/> and <see cref="Transaction"/>.</summary>
    bool SupportsExternalTransaction { get; }

    /// <summary>Active database connection for Dapper; not supported on <see cref="InMemoryArchiForgeUnitOfWork"/>.</summary>
    IDbConnection Connection { get; }

    /// <summary>Ambient transaction for Dapper (begun by the factory); not supported on <see cref="InMemoryArchiForgeUnitOfWork"/>.</summary>
    IDbTransaction Transaction { get; }

    /// <summary>Commits the transaction. Throws if the unit of work was already completed (Dapper).</summary>
    /// <param name="ct">Cancellation token (unused for sync ADO.NET commit; reserved for future use).</param>
    Task CommitAsync(CancellationToken ct);

    /// <summary>Rolls back the transaction if not yet completed (no-op if already completed).</summary>
    /// <param name="ct">Cancellation token (unused for sync ADO.NET rollback; reserved for future use).</param>
    Task RollbackAsync(CancellationToken ct);
}
