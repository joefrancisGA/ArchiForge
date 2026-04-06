using System.Text.Json;

using ArchiForge.Core.Audit;
using ArchiForge.Core.Diagnostics;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Persistence.Serialization;

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
        ArgumentNullException.ThrowIfNull(digest);

        IReadOnlyList<DigestSubscription> subscriptions = await subscriptionRepository
            .ListEnabledByScopeAsync(digest.TenantId, digest.WorkspaceId, digest.ProjectId, ct)
            ;

        foreach (DigestSubscription subscription in subscriptions)
        
            await DeliverToSubscriptionAsync(digest, subscription, ct);
        
    }

    /// <summary>
    /// Creates an attempt row, resolves the channel, sends the digest, then updates the attempt status and audits the result.
    /// </summary>
    /// <remarks>
    /// <see cref="OperationCanceledException"/> is re-thrown to allow callers to honour cancellation.
    /// All other exceptions are caught, recorded on the attempt row, and audited as failures without propagating.
    /// </remarks>
    private async Task DeliverToSubscriptionAsync(
        ArchitectureDigest digest,
        DigestSubscription subscription,
        CancellationToken ct)
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

        await attemptRepository.CreateAsync(attempt, ct);

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
                ;

            attempt.Status = DigestDeliveryStatus.Succeeded;
            subscription.LastDeliveredUtc = DateTime.UtcNow;

            await attemptRepository.UpdateAsync(attempt, ct);
            await subscriptionRepository.UpdateAsync(subscription, ct);

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.DigestDeliverySucceeded,
                    RunId = digest.RunId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            digestId = digest.DigestId,
                            subscriptionId = subscription.SubscriptionId,
                            channelType = subscription.ChannelType
                        },
                        AuditJsonSerializationOptions.Instance),
                },
                ct);

            ArchiForgeInstrumentation.DigestDeliverySucceeded.Add(
                1,
                new KeyValuePair<string, object?>("channel", subscription.ChannelType));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            attempt.Status = DigestDeliveryStatus.Failed;
            attempt.ErrorMessage = ex.Message;

            await attemptRepository.UpdateAsync(attempt, ct);

            await auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.DigestDeliveryFailed,
                    RunId = digest.RunId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            digestId = digest.DigestId,
                            subscriptionId = subscription.SubscriptionId,
                            channelType = subscription.ChannelType,
                            error = ex.Message
                        },
                        AuditJsonSerializationOptions.Instance),
                },
                ct);

            ArchiForgeInstrumentation.DigestDeliveryFailed.Add(
                1,
                new KeyValuePair<string, object?>("channel", subscription.ChannelType));
        }
    }
}
