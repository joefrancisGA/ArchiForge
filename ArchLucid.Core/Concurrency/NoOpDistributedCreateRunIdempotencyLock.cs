namespace ArchLucid.Core.Concurrency;

/// <summary>In-memory / single-instance hosts: no cross-replica lock (in-process serialization remains).</summary>
public sealed class NoOpDistributedCreateRunIdempotencyLock : IDistributedCreateRunIdempotencyLock
{
    /// <inheritdoc />
    public Task<IAsyncDisposable> AcquireExclusiveSessionLockAsync(
        string lockResourceName,
        int lockTimeoutMs,
        CancellationToken cancellationToken)
    {
        _ = lockResourceName;
        _ = lockTimeoutMs;

        return Task.FromResult<IAsyncDisposable>(EmptySessionLock.Instance);
    }

    private sealed class EmptySessionLock : IAsyncDisposable
    {
        public static readonly EmptySessionLock Instance = new();

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
