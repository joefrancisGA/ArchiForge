using ArchLucid.Decisioning.Advisory.Scheduling;

namespace ArchLucid.Decisioning.Advisory.Delivery;

/// <summary>Delivers an <see cref="ArchitectureDigest" /> to a Slack channel via an incoming webhook.</summary>
public sealed class DigestSlackWebhookDeliveryChannel(IWebhookPoster webhookPoster) : IDigestDeliveryChannel
{
    public string ChannelType => DigestDeliveryChannelType.SlackWebhook;

    public Task SendAsync(DigestDeliveryPayload payload, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payload);

        object body = new
        {
            text = $"*{payload.Digest.Title}*\n{payload.Digest.Summary}\n\n{payload.Digest.ContentMarkdown}"
        };

        return webhookPoster.PostJsonAsync(
            payload.Subscription.Destination,
            body,
            ct);
    }
}
