namespace ArchiForge.Decisioning.Alerts.Delivery;

/// <summary>
/// Sends one alert notification to a subscription destination (email address, webhook URL, etc.).
/// </summary>
/// <remarks>
/// Implementations are registered in DI and matched by <see cref="ChannelType"/> against <see cref="AlertRoutingSubscription.ChannelType"/>
/// (typically <see cref="AlertRoutingChannelType"/> constants). Invoked from <c>ArchiForge.Persistence.Alerts.AlertDeliveryDispatcher</c>.
/// </remarks>
public interface IAlertDeliveryChannel
{
    /// <summary>
    /// Discriminator matching <see cref="AlertRoutingChannelType"/> / subscription rows (case-insensitive match in dispatcher).
    /// </summary>
    string ChannelType
    {
        get;
    }

    /// <summary>
    /// Formats <paramref name="payload"/> for the channel and performs the send (HTTP, SMTP abstraction, etc.).
    /// </summary>
    /// <param name="payload">Alert plus the routing row that selected this channel.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync(
        AlertDeliveryPayload payload,
        CancellationToken ct);
}
