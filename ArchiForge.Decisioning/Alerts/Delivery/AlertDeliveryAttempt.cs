namespace ArchiForge.Decisioning.Alerts.Delivery;

public class AlertDeliveryAttempt
{
    public Guid AlertDeliveryAttemptId { get; set; } = Guid.NewGuid();
    public Guid AlertId { get; set; }
    public Guid RoutingSubscriptionId { get; set; }

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public DateTime AttemptedUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = AlertDeliveryAttemptStatus.Started;
    public string? ErrorMessage { get; set; }

    public string ChannelType { get; set; } = null!;
    public string Destination { get; set; } = null!;

    public int RetryCount { get; set; }
}
