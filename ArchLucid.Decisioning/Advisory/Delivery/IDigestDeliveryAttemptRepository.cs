namespace ArchLucid.Decisioning.Advisory.Delivery;

/// <summary>
///     Persistence for <see cref="DigestDeliveryAttempt" /> rows (one per subscription per digest send try).
/// </summary>
/// <remarks>
///     SQL: <c>DapperDigestDeliveryAttemptRepository</c> on <c>dbo.DigestDeliveryAttempts</c>. Written by
///     <c>DigestDeliveryDispatcher</c>.
/// </remarks>
public interface IDigestDeliveryAttemptRepository
{
    /// <summary>Creates a row in <see cref="DigestDeliveryStatus.Started" /> before the channel send.</summary>
    Task CreateAsync(DigestDeliveryAttempt attempt, CancellationToken ct);

    /// <summary>Updates status and error after success or failure.</summary>
    Task UpdateAsync(DigestDeliveryAttempt attempt, CancellationToken ct);

    /// <summary>Attempts for one digest (newest first per repository ordering).</summary>
    Task<IReadOnlyList<DigestDeliveryAttempt>> ListByDigestAsync(
        Guid digestId,
        CancellationToken ct);

    /// <summary>Recent attempts for a subscription (operator diagnostics).</summary>
    Task<IReadOnlyList<DigestDeliveryAttempt>> ListBySubscriptionAsync(
        Guid subscriptionId,
        int take,
        CancellationToken ct);
}
