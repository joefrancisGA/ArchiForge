using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Decisioning.Advisory.Delivery;

/// <summary>Delivers an <see cref="ArchitectureDigest"/> to a subscriber via e-mail using <see cref="IEmailSender"/>.</summary>
public sealed class DigestEmailDeliveryChannel(IEmailSender emailSender) : IDigestDeliveryChannel
{
    public string ChannelType => DigestDeliveryChannelType.Email;

    public Task SendAsync(DigestDeliveryPayload payload, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payload);

        string subject = payload.Digest.Title;
        string body = $"{payload.Digest.Summary}{Environment.NewLine}{Environment.NewLine}{payload.Digest.ContentMarkdown}";

        return emailSender.SendAsync(
            payload.Subscription.Destination,
            subject,
            body,
            ct);
    }
}
