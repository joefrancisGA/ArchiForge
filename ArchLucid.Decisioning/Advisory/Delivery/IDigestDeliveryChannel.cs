namespace ArchiForge.Decisioning.Advisory.Delivery;

/// <summary>
/// Sends one architecture digest to a subscription destination (parallel concept to <c>IAlertDeliveryChannel</c>).
/// </summary>
public interface IDigestDeliveryChannel
{
    /// <summary>Matches <see cref="DigestDeliveryChannelType"/> / subscription rows.</summary>
    string ChannelType { get; }

    /// <summary>Formats and sends the digest (email body, webhook JSON, etc.).</summary>
    Task SendAsync(
        DigestDeliveryPayload payload,
        CancellationToken ct);
}
