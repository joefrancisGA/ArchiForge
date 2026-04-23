namespace ArchLucid.Core.Concurrency;

/// <summary>
///     Cross-replica mutual exclusion for create-run idempotency (for example SQL Server <c>sp_getapplock</c> with session
///     owner).
/// </summary>
public interface IDistributedCreateRunIdempotencyLock
{
    /// <summary>
    ///     Acquires an exclusive session-scoped application lock for <paramref name="lockResourceName" />.
    /// </summary>
    /// <param name="lockResourceName">Stable resource name (implementation may hash/truncate for provider limits).</param>
    /// <param name="lockTimeoutMs">Provider lock wait budget in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Releases the lock when disposed.</returns>
    /// <exception cref="TimeoutException">Thrown when the lock cannot be acquired within <paramref name="lockTimeoutMs" />.</exception>
    Task<IAsyncDisposable> AcquireExclusiveSessionLockAsync(
        string lockResourceName,
        int lockTimeoutMs,
        CancellationToken cancellationToken);
}
