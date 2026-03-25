using System.Text.Json;

using ArchiForge.Core.Audit;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Persistence.Advisory;

/// <summary>
/// Default <see cref="IDigestDeliveryDispatcher"/>: loads enabled digest subscriptions, records attempts, invokes <see cref="IDigestDeliveryChannel"/>s, and audits per subscription.
/// </summary>
/// <param name="channels">Registered digest channels (email, Teams, Slack, …).</param>
/// <param name="subscriptionRepository">Scope-scoped routing rows.</param>
/// <param name="attemptRepository">Persists delivery attempt lifecycle.</param>
/// <param name="auditService">Success/failure audit events.</param>
public sealed class DigestDeliveryDispatcher(
    IEnumerable<IDigestDeliveryChannel> channels,
    IDigestSubscriptionRepository subscriptionRepository,
    IDigestDeliveryAttemptRepository attemptRepository,
    IAuditService auditService) : IDigestDeliveryDispatcher
{
    private static IDigestDeliveryChannel ResolveChannel(IEnumerable<IDigestDeliveryChannel> channels, string channelType) =>
        channels.FirstOrDefault(x => string.Equals(x.ChannelType, channelType, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"No delivery channel registered for '{channelType}'.");

    /// <inheritdoc />
    public async Task DeliverAsync(ArchitectureDigest digest, CancellationToken ct)
    {
        IReadOnlyList<DigestSubscription> subscriptions = await subscriptionRepository
            .ListEnabledByScopeAsync(digest.TenantId, digest.WorkspaceId, digest.ProjectId, ct)
            .ConfigureAwait(false);

        foreach (DigestSubscription subscription in subscriptions)
        {
            DigestDeliveryAttempt attempt = new()
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
                IDigestDeliveryChannel channel = ResolveChannel(channels, subscription.ChannelType);

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
            catch (OperationCanceledException)
            {
                throw;
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
