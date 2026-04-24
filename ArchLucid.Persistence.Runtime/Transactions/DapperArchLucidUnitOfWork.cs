using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Transactions;

namespace ArchLucid.Persistence.Transactions;

/// <summary>
///     Dapper-backed unit of work holding an open <see cref="IDbConnection" /> and <see cref="IDbTransaction" />;
///     <see cref="CommitAsync" /> commits once, <see cref="RollbackAsync" /> rolls back, and <see cref="DisposeAsync" />
///     rolls back if still pending then disposes both.
/// </summary>
[ExcludeFromCodeCoverage(Justification =
    "Wraps IDbConnection/IDbTransaction commit/rollback lifecycle; requires live database transaction.")]
public sealed class DapperArchLucidUnitOfWork(IDbConnection connection, IDbTransaction transaction)
    : IArchLucidUnitOfWork
{
    private bool _completed;

    /// <inheritdoc />
    public bool SupportsExternalTransaction => true;

    /// <inheritdoc />
    public IDbConnection Connection
    {
        get;
    } = connection;

    /// <inheritdoc />
    public IDbTransaction Transaction
    {
        get;
    } = transaction;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    ///     Thrown when commit is called after the unit of work has already been
    ///     completed.
    /// </exception>
    public Task CommitAsync(CancellationToken ct)
    {
        _ = ct;
        if (_completed)
            throw new InvalidOperationException("Unit of work has already been completed.");

        Transaction.Commit();
        _completed = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RollbackAsync(CancellationToken ct)
    {
        _ = ct;
        if (_completed)
            return Task.CompletedTask;

        Transaction.Rollback();
        _completed = true;
        return Task.CompletedTask;
    }

    /// <summary>Best-effort rollback if not completed, then disposes transaction and connection.</summary>
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
