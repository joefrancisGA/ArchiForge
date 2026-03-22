namespace ArchiForge.Decisioning.Advisory.Delivery;

public class DigestDeliveryAttempt
{
    public Guid AttemptId { get; set; } = Guid.NewGuid();
    public Guid DigestId { get; set; }
    public Guid SubscriptionId { get; set; }

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public DateTime AttemptedUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Started";
    public string? ErrorMessage { get; set; }

    public string ChannelType { get; set; } = null!;
    public string Destination { get; set; } = null!;
}
