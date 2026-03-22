using ArchiForge.Decisioning.Alerts;

namespace ArchiForge.Decisioning.Alerts.Delivery;

public class AlertRoutingSubscription
{
    public Guid RoutingSubscriptionId { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = "Alert Routing Subscription";
    public string ChannelType { get; set; } = null!;
    public string Destination { get; set; } = null!;

    public string MinimumSeverity { get; set; } = AlertSeverity.Warning;
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastDeliveredUtc { get; set; }

    public string MetadataJson { get; set; } = "{}";
}
