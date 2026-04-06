using ArchiForge.Decisioning.Advisory.Delivery;

namespace ArchiForge.Decisioning.Alerts.Delivery;

/// <summary>
/// Posts a structured JSON payload suitable for generic on-call or paging endpoints (<see cref="AlertRoutingChannelType.OnCallWebhook"/>).
/// </summary>
/// <param name="webhookPoster">HTTP JSON POST helper.</param>
public sealed class AlertOnCallWebhookDeliveryChannel(IWebhookPoster webhookPoster) : IAlertDeliveryChannel
{
    /// <inheritdoc />
    public string ChannelType => AlertRoutingChannelType.OnCallWebhook;

    /// <inheritdoc />
    public Task SendAsync(AlertDeliveryPayload payload, CancellationToken ct)
    {
        var body = new
        {
            severity = payload.Alert.Severity,
            title = payload.Alert.Title,
            category = payload.Alert.Category,
            triggerValue = payload.Alert.TriggerValue,
            description = payload.Alert.Description,
            alertId = payload.Alert.AlertId,
            runId = payload.Alert.RunId,
        };

        return webhookPoster.PostJsonAsync(
            payload.Subscription.Destination,
            body,
            ct);
    }
}
