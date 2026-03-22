namespace ArchiForge.Decisioning.Advisory.Delivery;

public sealed class DigestSlackWebhookDeliveryChannel(IWebhookPoster webhookPoster) : IDigestDeliveryChannel
{
    public string ChannelType => DigestDeliveryChannelType.SlackWebhook;

    public Task SendAsync(DigestDeliveryPayload payload, CancellationToken ct)
    {
        var body = new
        {
            text = $"*{payload.Digest.Title}*\n{payload.Digest.Summary}\n\n{payload.Digest.ContentMarkdown}"
        };

        return webhookPoster.PostJsonAsync(
            payload.Subscription.Destination,
            body,
            ct);
    }
}
