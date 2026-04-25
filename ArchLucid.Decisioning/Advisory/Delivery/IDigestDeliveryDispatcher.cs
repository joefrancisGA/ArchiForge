using ArchLucid.Decisioning.Advisory.Scheduling;

namespace ArchLucid.Decisioning.Advisory.Delivery;

/// <summary>
///     Fans out a completed <see cref="ArchitectureDigest" /> to all enabled <see cref="DigestSubscription" /> rows for
///     its scope.
/// </summary>
/// <remarks>
///     Implemented by <c>ArchLucid.Persistence.Advisory.DigestDeliveryDispatcher</c>. Called from
///     <c>AdvisoryScanRunner</c> after digest persistence.
/// </remarks>
public interface IDigestDeliveryDispatcher
{
    /// <summary>
    ///     For each enabled subscription, creates an attempt row, resolves <see cref="IDigestDeliveryChannel" />, sends,
    ///     updates status, and audits.
    /// </summary>
    /// <param name="digest">Digest already stored for the scope.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeliverAsync(ArchitectureDigest digest, CancellationToken ct);
}
