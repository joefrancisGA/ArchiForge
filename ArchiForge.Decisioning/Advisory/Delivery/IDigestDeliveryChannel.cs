namespace ArchiForge.Decisioning.Advisory.Delivery;

public interface IDigestDeliveryChannel
{
    string ChannelType { get; }

    Task SendAsync(
        DigestDeliveryPayload payload,
        CancellationToken ct);
}
