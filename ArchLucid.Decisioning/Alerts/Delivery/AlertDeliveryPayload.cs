namespace ArchLucid.Decisioning.Alerts.Delivery;

/// <summary>
///     Input to <see cref="IAlertDeliveryChannel.SendAsync" />: the persisted alert and the subscription that matched
///     severity routing.
/// </summary>
public class AlertDeliveryPayload
{
    /// <summary>Fired alert row (title, severity, description, run linkage).</summary>
    public AlertRecord Alert
    {
        get;
        set;
    } = null!;

    /// <summary>Routing configuration (destination URL/email, channel type, minimum severity).</summary>
    public AlertRoutingSubscription Subscription
    {
        get;
        set;
    } = null!;
}
