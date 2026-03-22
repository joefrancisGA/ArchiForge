using ArchiForge.Decisioning.Alerts;

namespace ArchiForge.Decisioning.Alerts.Delivery;

public class AlertDeliveryPayload
{
    public AlertRecord Alert { get; set; } = null!;
    public AlertRoutingSubscription Subscription { get; set; } = null!;
}
