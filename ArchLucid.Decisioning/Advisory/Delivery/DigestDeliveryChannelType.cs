namespace ArchLucid.Decisioning.Advisory.Delivery;

/// <summary>
///     <see cref="IDigestDeliveryChannel.ChannelType" /> and <see cref="DigestSubscription.ChannelType" /> discriminator
///     strings.
/// </summary>
public static class DigestDeliveryChannelType
{
    public const string Email = "Email";
    public const string TeamsWebhook = "TeamsWebhook";
    public const string SlackWebhook = "SlackWebhook";
}
