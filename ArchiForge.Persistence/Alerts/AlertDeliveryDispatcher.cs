using System.Text.Json;

using ArchiForge.Core.Audit;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Delivery;

namespace ArchiForge.Persistence.Alerts;

/// <summary>
/// Default <see cref="IAlertDeliveryDispatcher"/>: loads routing subscriptions, matches severity, sends through registered <see cref="IAlertDeliveryChannel"/>s, and tracks <see cref="AlertDeliveryAttempt"/> rows.
/// </summary>
/// <param name="channels">All registered delivery channels (e.g. email, Slack).</param>
/// <param name="subscriptionRepository">Per-scope routing configuration.</param>
/// <param name="attemptRepository">Persists per-try delivery state.</param>
/// <param name="auditService">Logs delivery success and failure.</param>
/// <remarks>
/// Skips silently when no subscriptions match; each matching subscription yields one attempt row and one channel send.
/// </remarks>
public sealed class AlertDeliveryDispatcher(
    IEnumerable<IAlertDeliveryChannel> channels,
    IAlertRoutingSubscriptionRepository subscriptionRepository,
    IAlertDeliveryAttemptRepository attemptRepository,
    IAuditService auditService) : IAlertDeliveryDispatcher
{
    private static IAlertDeliveryChannel ResolveChannel(IEnumerable<IAlertDeliveryChannel> channels, string channelType) =>
        channels.FirstOrDefault(x => string.Equals(x.ChannelType, channelType, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"No alert delivery channel registered for '{channelType}'.");

    /// <inheritdoc />
    public async Task DeliverAsync(AlertRecord alert, CancellationToken ct)
    {
        var subscriptions = await subscriptionRepository
            .ListEnabledByScopeAsync(alert.TenantId, alert.WorkspaceId, alert.ProjectId, ct)
            .ConfigureAwait(false);

        var matching = subscriptions
            .Where(x => AlertSeverityComparer.MeetsMinimum(alert.Severity, x.MinimumSeverity))
            .ToList();

        foreach (var subscription in matching)
        {
            var attempt = new AlertDeliveryAttempt
            {
                AlertDeliveryAttemptId = Guid.NewGuid(),
                AlertId = alert.AlertId,
                RoutingSubscriptionId = subscription.RoutingSubscriptionId,
                TenantId = alert.TenantId,
                WorkspaceId = alert.WorkspaceId,
                ProjectId = alert.ProjectId,
                AttemptedUtc = DateTime.UtcNow,
                Status = AlertDeliveryAttemptStatus.Started,
                ChannelType = subscription.ChannelType,
                Destination = subscription.Destination,
                RetryCount = 0,
            };

            await attemptRepository.CreateAsync(attempt, ct).ConfigureAwait(false);

            try
            {
                var channel = ResolveChannel(channels, subscription.ChannelType);

                await channel
                    .SendAsync(
                        new AlertDeliveryPayload
                        {
                            Alert = alert,
                            Subscription = subscription,
                        },
                        ct)
                    .ConfigureAwait(false);

                attempt.Status = AlertDeliveryAttemptStatus.Succeeded;
                subscription.LastDeliveredUtc = DateTime.UtcNow;

                await attemptRepository.UpdateAsync(attempt, ct).ConfigureAwait(false);
                await subscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

                await auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.AlertDeliverySucceeded,
                        RunId = alert.RunId,
                        DataJson = JsonSerializer.Serialize(new
                        {
                            alertId = alert.AlertId,
                            subscription.RoutingSubscriptionId,
                            subscription.ChannelType,
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
                attempt.Status = AlertDeliveryAttemptStatus.Failed;
                attempt.ErrorMessage = ex.Message;
                await attemptRepository.UpdateAsync(attempt, ct).ConfigureAwait(false);

                await auditService.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.AlertDeliveryFailed,
                        RunId = alert.RunId,
                        DataJson = JsonSerializer.Serialize(new
                        {
                            alertId = alert.AlertId,
                            subscription.RoutingSubscriptionId,
                            subscription.ChannelType,
                            error = ex.Message,
                        }),
                    },
                    ct).ConfigureAwait(false);
            }
        }
    }
}
