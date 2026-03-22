using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Decisioning.Advisory.Delivery;

public class DigestDeliveryPayload
{
    public ArchitectureDigest Digest { get; set; } = default!;
    public DigestSubscription Subscription { get; set; } = default!;
}
