namespace ArchLucid.Decisioning.Alerts.Delivery;

/// <summary>
///     <see cref="IAlertDeliveryChannel.ChannelType" /> and <see cref="AlertRoutingSubscription.ChannelType" />
///     discriminator strings.
/// </summary>
public static class AlertRoutingChannelType
{
    public const string Email = "Email";
    public const string TeamsWebhook = "TeamsWebhook";
    public const string SlackWebhook = "SlackWebhook";
    public const string OnCallWebhook = "OnCallWebhook";
}
