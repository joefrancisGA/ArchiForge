using System.Text.Json;
using ArchiForge.Core.Audit;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Persistence.Advisory;

public sealed class DigestDeliveryDispatcher(
    IEnumerable<IDigestDeliveryChannel> channels,
    IDigestSubscriptionRepository subscriptionRepository,
    IDigestDeliveryAttemptRepository attemptRepository,
    IAuditService auditService) : IDigestDeliveryDispatcher
{
    public async Task DeliverAsync(ArchitectureDigest digest, CancellationToken ct)
    {
        var subscriptions = await subscriptionRepository
            .ListEnabledByScopeAsync(digest.TenantId, digest.WorkspaceId, digest.ProjectId, ct)
            .ConfigureAwait(false);

        foreach (var subscription in subscriptions)
        {
            var attempt = new DigestDeliveryAttempt
            {
                AttemptId = Guid.NewGuid(),
                DigestId = digest.DigestId,
                SubscriptionId = subscription.SubscriptionId,
                TenantId = digest.TenantId,
                WorkspaceId = digest.WorkspaceId,
                ProjectId = digest.ProjectId,
                AttemptedUtc = DateTime.UtcNow,
                Status = DigestDeliveryStatus.Started,
                ChannelType = subscription.ChannelType,
                Destination = subscription.Destination
            };

            await attemptRepository.CreateAsync(attempt, ct).ConfigureAwait(false);

            try
            {
                var channel = channels.FirstOrDefault(x =>
                    string.Equals(x.ChannelType, subscription.ChannelType, StringComparison.OrdinalIgnoreCase));

                if (channel is null)
                    throw new InvalidOperationException($"No delivery channel registered for {subscription.ChannelType}.");

                await channel
                    .SendAsync(
                        new DigestDeliveryPayload
                        {
                            Digest = digest,
                            Subscription = subscription
                        },
                        ct)
                    .ConfigureAwait(false);

                attempt.Status = DigestDeliveryStatus.Succeeded;
                subscription.LastDeliveredUtc = DateTime.UtcNow;

                await attemptRepository.UpdateAsync(attempt, ct).ConfigureAwait(false);
                await subscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

                await auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.DigestDeliverySucceeded,
                        RunId = digest.RunId,
                        DataJson = JsonSerializer.Serialize(new
                        {
                            digestId = digest.DigestId,
                            subscriptionId = subscription.SubscriptionId,
                            channelType = subscription.ChannelType
                        }),
                    },
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                attempt.Status = DigestDeliveryStatus.Failed;
                attempt.ErrorMessage = ex.Message;
                await attemptRepository.UpdateAsync(attempt, ct).ConfigureAwait(false);

                await auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.DigestDeliveryFailed,
                        RunId = digest.RunId,
                        DataJson = JsonSerializer.Serialize(new
                        {
                            digestId = digest.DigestId,
                            subscriptionId = subscription.SubscriptionId,
                            channelType = subscription.ChannelType,
                            error = ex.Message
                        }),
                    },
                    ct).ConfigureAwait(false);
            }
        }
    }
}
