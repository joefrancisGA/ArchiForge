namespace ArchLucid.Core.Audit;

/// <summary>
///     Bounded in-memory queue for durable audit events that failed on the hot path (e.g. circuit-breaker callback).
/// </summary>
public interface IAuditRetryQueue
{
    /// <summary>Approximate number of events not yet successfully written to durable audit storage.</summary>
    long ApproximatePendingCount
    {
        get;
    }

    /// <summary>
    ///     Attempts to enqueue a copy of <paramref name="auditEvent" />; returns <see langword="false" /> when the queue
    ///     is full.
    /// </summary>
    bool TryEnqueue(AuditEvent auditEvent);

    /// <summary>Blocks until an event is available or the token is cancelled.</summary>
    ValueTask<AuditEvent> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>Call after <see cref="IAuditService.LogAsync" /> succeeds for a dequeued event.</summary>
    void NotifyPersistedSuccess();

    /// <summary>
    ///     After a failed drain, attempts to put the event back without changing <see cref="ApproximatePendingCount" />.
    ///     Returns <see langword="false" /> if the channel is full (caller should treat the event as dropped).
    /// </summary>
    bool TryReturnToQueueAfterFailedDrain(AuditEvent auditEvent);
}
