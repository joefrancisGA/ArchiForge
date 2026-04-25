using ArchLucid.Persistence.Cosmos;

namespace ArchLucid.Persistence.Audit;

/// <summary>
///     Receives batches of audit documents from the Cosmos change feed (at-least-once; implementations must be
///     idempotent).
/// </summary>
public interface IAuditEventChangeFeedHandler
{
    Task HandleAsync(IReadOnlyList<AuditEventDocument> changes, CancellationToken cancellationToken);
}
