namespace ArchiForge.Decisioning.Advisory.Delivery;

public sealed class DigestTeamsWebhookDeliveryChannel(IWebhookPoster webhookPoster) : IDigestDeliveryChannel
{
    public string ChannelType => DigestDeliveryChannelType.TeamsWebhook;

    public Task SendAsync(DigestDeliveryPayload payload, CancellationToken ct)
    {
        var body = new
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
