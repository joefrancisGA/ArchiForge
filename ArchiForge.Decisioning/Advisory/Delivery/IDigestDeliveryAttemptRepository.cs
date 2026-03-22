namespace ArchiForge.Decisioning.Advisory.Delivery;

public interface IDigestDeliveryAttemptRepository
{
    Task CreateAsync(DigestDeliveryAttempt attempt, CancellationToken ct);
    Task UpdateAsync(DigestDeliveryAttempt attempt, CancellationToken ct);

    Task<IReadOnlyList<DigestDeliveryAttempt>> ListByDigestAsync(
        Guid digestId,
        CancellationToken ct);

    Task<IReadOnlyList<DigestDeliveryAttempt>> ListBySubscriptionAsync(
        Guid subscriptionId,
        int take,
        CancellationToken ct);
}
