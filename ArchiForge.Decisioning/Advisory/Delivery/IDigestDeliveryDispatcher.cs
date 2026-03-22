using ArchiForge.Decisioning.Advisory.Scheduling;

namespace ArchiForge.Decisioning.Advisory.Delivery;

public interface IDigestDeliveryDispatcher
{
    Task DeliverAsync(ArchitectureDigest digest, CancellationToken ct);
}
