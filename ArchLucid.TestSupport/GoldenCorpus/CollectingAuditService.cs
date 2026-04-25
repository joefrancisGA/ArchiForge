using ArchLucid.Core.Audit;

namespace ArchLucid.TestSupport.GoldenCorpus;

/// <summary>In-memory <see cref="IAuditService" /> for golden-corpus regression: records event type strings in order.</summary>
public sealed class CollectingAuditService : IAuditService
{
    private readonly List<string> _eventTypes = [];

    /// <summary>Immutable snapshot of recorded <see cref="AuditEvent.EventType" /> values (append order).</summary>
    public IReadOnlyList<string> EventTypes => _eventTypes;

    /// <inheritdoc />
    public Task LogAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(auditEvent.EventType))
            throw new InvalidOperationException("Audit event type is required.");

        _eventTypes.Add(auditEvent.EventType.Trim());
        return Task.CompletedTask;
    }
}
