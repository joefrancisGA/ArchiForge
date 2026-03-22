using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Decisioning.Advisory.Delivery;

public class DigestDeliveryPayload
{
    public ArchitectureDigest Digest { get; set; } = null!;
    public DigestSubscription Subscription { get; set; } = null!;
}
