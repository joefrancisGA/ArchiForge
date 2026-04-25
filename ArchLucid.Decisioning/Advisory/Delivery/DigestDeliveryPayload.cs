using ArchLucid.Decisioning.Advisory.Scheduling;

namespace ArchLucid.Decisioning.Advisory.Delivery;

/// <summary>
///     Input to <see cref="IDigestDeliveryChannel.SendAsync" />: digest body plus the subscription that matched the scope.
/// </summary>
public class DigestDeliveryPayload
{
    /// <summary>Architecture digest produced by the advisory scan pipeline.</summary>
    public ArchitectureDigest Digest
    {
        get;
        set;
    } = null!;

    /// <summary>Routing row (destination, channel type).</summary>
    public DigestSubscription Subscription
    {
        get;
        set;
    } = null!;
}
