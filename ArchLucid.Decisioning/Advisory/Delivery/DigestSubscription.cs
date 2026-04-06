namespace ArchiForge.Decisioning.Advisory.Delivery;

/// <summary>
/// Outbound route for architecture digests: channel type, destination, and enablement for a scope.
/// </summary>
/// <remarks>
/// <see cref="ChannelType"/> should align with <see cref="DigestDeliveryChannelType"/> and a registered <see cref="IDigestDeliveryChannel"/>.
/// </remarks>
public class DigestSubscription
{
    /// <summary>Primary key; assigned on API create.</summary>
    public Guid SubscriptionId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant owning the subscription.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Workspace within the tenant.</summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>Project within the workspace.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Operator-facing label.</summary>
    public string Name { get; set; } = "Digest Subscription";

    /// <summary>E.g. <see cref="DigestDeliveryChannelType.Email"/>.</summary>
    public string ChannelType { get; set; } = null!;

    /// <summary>Email address or webhook URL.</summary>
    public string Destination { get; set; } = null!;

    /// <summary>When false, excluded from <see cref="IDigestSubscriptionRepository.ListEnabledByScopeAsync"/>.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Row creation time (UTC).</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Updated after a successful channel send.</summary>
    public DateTime? LastDeliveredUtc { get; set; }

    /// <summary>Opaque JSON for future channel options.</summary>
    public string MetadataJson { get; set; } = "{}";
}
