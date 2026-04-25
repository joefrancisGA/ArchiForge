using System.Threading.Channels;

using ArchLucid.Core.Diagnostics;

namespace ArchLucid.Core.Audit;

/// <summary>
///     Thread-safe bounded channel for audit retry; drops with a metric when full.
/// </summary>
public sealed class InMemoryAuditRetryQueue : IAuditRetryQueue
{
    private const int DefaultCapacity = 1000;

    private readonly Channel<AuditEvent> _channel;

    private int _pending;

    /// <summary>Creates a queue with the default capacity (<see cref="DefaultCapacity" />).</summary>
    public InMemoryAuditRetryQueue()
        : this(DefaultCapacity)
    {
    }

    /// <param name="capacity">Maximum queued events before <see cref="TryEnqueue" /> fails.</param>
    public InMemoryAuditRetryQueue(int capacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        BoundedChannelOptions options = new(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true, SingleWriter = false
        };

        _channel = Channel.CreateBounded<AuditEvent>(options);
        ArchLucidInstrumentation.SetAuditRetryQueuePendingReader(() => Volatile.Read(ref _pending));
    }

    /// <inheritdoc />
    public long ApproximatePendingCount => Volatile.Read(ref _pending);

    /// <inheritdoc />
    public bool TryEnqueue(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        AuditEvent copy = CopyAuditEvent(auditEvent);

        if (!_channel.Writer.TryWrite(copy))
        {
            ArchLucidInstrumentation.AuditRetryEnqueueDroppedTotal.Add(1);

            return false;
        }

        _ = Interlocked.Increment(ref _pending);

        return true;
    }

    /// <inheritdoc />
    public ValueTask<AuditEvent> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void NotifyPersistedSuccess()
    {
        _ = Interlocked.Decrement(ref _pending);
    }

    /// <inheritdoc />
    public bool TryReturnToQueueAfterFailedDrain(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        AuditEvent copy = CopyAuditEvent(auditEvent);

        if (_channel.Writer.TryWrite(copy))
            return true;


        ArchLucidInstrumentation.AuditRetryEnqueueDroppedTotal.Add(1);

        _ = Interlocked.Decrement(ref _pending);

        return false;
    }

    private static AuditEvent CopyAuditEvent(AuditEvent source)
    {
        return new AuditEvent
        {
            EventId = source.EventId,
            OccurredUtc = source.OccurredUtc,
            EventType = source.EventType,
            ActorUserId = source.ActorUserId,
            ActorUserName = source.ActorUserName,
            TenantId = source.TenantId,
            WorkspaceId = source.WorkspaceId,
            ProjectId = source.ProjectId,
            RunId = source.RunId,
            ManifestId = source.ManifestId,
            ArtifactId = source.ArtifactId,
            DataJson = source.DataJson,
            CorrelationId = source.CorrelationId
        };
    }
}
