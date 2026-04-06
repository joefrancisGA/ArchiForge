using ArchiForge.Decisioning.Advisory.Delivery;

namespace ArchiForge.Decisioning.Alerts.Delivery;

/// <summary>
/// Posts a simple <c>text</c> payload to a Slack incoming webhook (<see cref="AlertRoutingChannelType.SlackWebhook"/>).
/// </summary>
/// <param name="webhookPoster">HTTP JSON POST helper shared with advisory delivery.</param>
public sealed class AlertSlackWebhookDeliveryChannel(IWebhookPoster webhookPoster) : IAlertDeliveryChannel
{
    /// <inheritdoc />
    public string ChannelType => AlertRoutingChannelType.SlackWebhook;

    /// <inheritdoc />
    public Task SendAsync(AlertDeliveryPayload payload, CancellationToken ct)
    {
        var body = new
        {
            text =
                $"*[{payload.Alert.Severity}]* {payload.Alert.Title}\n" +
                $"Category: {payload.Alert.Category}\n" +
                $"Trigger: {payload.Alert.TriggerValue}\n\n" +
                $"{payload.Alert.Description}",
        };

        return webhookPoster.PostJsonAsync(
            payload.Subscription.Destination,
            body,
            ct);
    }
}
