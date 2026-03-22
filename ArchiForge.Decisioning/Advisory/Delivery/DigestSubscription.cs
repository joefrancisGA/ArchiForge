namespace ArchiForge.Decisioning.Advisory.Delivery;

public class DigestSubscription
{
    public Guid SubscriptionId { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = "Digest Subscription";
    public string ChannelType { get; set; } = default!;
    public string Destination { get; set; } = default!;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastDeliveredUtc { get; set; }

    public string MetadataJson { get; set; } = "{}";
}
