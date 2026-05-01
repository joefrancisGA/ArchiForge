namespace ArchLucid.Application.Common;

/// <summary>
///     Corrected 51R structured logging for baseline mutations. This is log discipline only—distinct from the host
///     <c>IAuditService</c> that persists <c>AuditEvents</c> to SQL (<c>ArchLucid.Core.Audit</c>).
/// </summary>
public interface IBaselineMutationAuditService
{
    /// <summary>
    ///     Emits a single structured information log for a baseline mutation outcome.
    /// </summary>
    /// <param name="eventType">Stable event name (prefer <c>ArchLucid.Core.Audit.AuditEventTypes.Baseline.*</c> constants).</param>
    /// <param name="actor">Non-empty actor string (use <see cref="IActorContext" />).</param>
    /// <param name="entityId">Primary entity identifier (e.g. run id).</param>
    /// <param name="details">Optional short text; large payloads must not be logged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAsync(
        string eventType,
        string actor,
        string entityId,
        string? details = null,
        CancellationToken cancellationToken = default);
}
