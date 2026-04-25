using ArchLucid.Decisioning.Advisory.Delivery;

namespace ArchLucid.Decisioning.Alerts.Delivery;

/// <summary>
///     Posts JSON with <c>title</c> and <c>text</c> to a Microsoft Teams incoming webhook (
///     <see cref="AlertRoutingChannelType.TeamsWebhook" />).
/// </summary>
/// <param name="webhookPoster">HTTP JSON POST helper.</param>
public sealed class AlertTeamsWebhookDeliveryChannel(IWebhookPoster webhookPoster) : IAlertDeliveryChannel
{
    /// <inheritdoc />
    public string ChannelType => AlertRoutingChannelType.TeamsWebhook;

    /// <inheritdoc />
    public Task SendAsync(AlertDeliveryPayload payload, CancellationToken ct)
    {
        var body = new
        {
            title = $"[{payload.Alert.Severity}] {payload.Alert.Title}",
            text =
                $"Category: {payload.Alert.Category}\n" +
                $"Trigger: {payload.Alert.TriggerValue}\n\n" +
                $"{payload.Alert.Description}"
        };

        return webhookPoster.PostJsonAsync(
            payload.Subscription.Destination,
            body,
            ct);
    }
}
