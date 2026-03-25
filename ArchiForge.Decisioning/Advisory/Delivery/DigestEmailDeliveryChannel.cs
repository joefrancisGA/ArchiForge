namespace ArchiForge.Decisioning.Advisory.Delivery;

public sealed class DigestEmailDeliveryChannel(IEmailSender emailSender) : IDigestDeliveryChannel
{
    public string ChannelType => DigestDeliveryChannelType.Email;

    public Task SendAsync(DigestDeliveryPayload payload, CancellationToken ct)
    {
        string subject = payload.Digest.Title;
        string body = $"{payload.Digest.Summary}{Environment.NewLine}{Environment.NewLine}{payload.Digest.ContentMarkdown}";

        return emailSender.SendAsync(
            payload.Subscription.Destination,
            subject,
            body,
            ct);
    }
}
