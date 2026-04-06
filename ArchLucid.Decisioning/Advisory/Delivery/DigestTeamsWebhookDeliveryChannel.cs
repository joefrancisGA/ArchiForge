using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Decisioning.Advisory.Delivery;

/// <summary>Delivers an <see cref="ArchitectureDigest"/> to a Microsoft Teams channel via an incoming webhook.</summary>
public sealed class DigestTeamsWebhookDeliveryChannel(IWebhookPoster webhookPoster) : IDigestDeliveryChannel
{
    public string ChannelType => DigestDeliveryChannelType.TeamsWebhook;

    public Task SendAsync(DigestDeliveryPayload payload, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payload);

        object body = new
        {
            title = payload.Digest.Title,
            text = $"{payload.Digest.Summary}\n\n{payload.Digest.ContentMarkdown}"
        };

        return webhookPoster.PostJsonAsync(
            payload.Subscription.Destination,
            body,
            ct);
    }
}
