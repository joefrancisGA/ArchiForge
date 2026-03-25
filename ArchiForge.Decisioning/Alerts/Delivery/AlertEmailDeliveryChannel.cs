using ArchiForge.Decisioning.Advisory.Delivery;

namespace ArchiForge.Decisioning.Alerts.Delivery;

/// <summary>
/// <see cref="IAlertDeliveryChannel"/> for <see cref="AlertRoutingChannelType.Email"/>; sends plain-text body via <see cref="IEmailSender"/>.
/// </summary>
/// <param name="emailSender">Configured SMTP or provider abstraction from the host.</param>
public sealed class AlertEmailDeliveryChannel(IEmailSender emailSender) : IAlertDeliveryChannel
{
    /// <inheritdoc />
    public string ChannelType => AlertRoutingChannelType.Email;

    /// <inheritdoc />
    public Task SendAsync(AlertDeliveryPayload payload, CancellationToken ct)
    {
        string subject = $"[{payload.Alert.Severity}] {payload.Alert.Title}";
        string body =
            $"Category: {payload.Alert.Category}{Environment.NewLine}" +
            $"Severity: {payload.Alert.Severity}{Environment.NewLine}" +
            $"Trigger: {payload.Alert.TriggerValue}{Environment.NewLine}{Environment.NewLine}" +
            $"{payload.Alert.Description}";

        return emailSender.SendAsync(
            payload.Subscription.Destination,
            subject,
            body,
            ct);
    }
}
