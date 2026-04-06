namespace ArchiForge.Decisioning.Advisory.Delivery;

/// <summary>
/// Audit row for one digest delivery try to a specific <see cref="DigestSubscription"/>.
/// </summary>
public class DigestDeliveryAttempt
{
    /// <summary>Attempt primary key.</summary>
    public Guid AttemptId { get; set; } = Guid.NewGuid();

    /// <summary>Digest that was being delivered.</summary>
    public Guid DigestId { get; set; }

    /// <summary>Subscription used for this attempt.</summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>Scope copied from the digest.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Scope copied from the digest.</summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>Scope copied from the digest.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>When the attempt started (UTC).</summary>
    public DateTime AttemptedUtc { get; set; } = DateTime.UtcNow;

    /// <summary><see cref="DigestDeliveryStatus"/> value.</summary>
    public string Status { get; set; } = DigestDeliveryStatus.Started;

    /// <summary>Set when <see cref="Status"/> is <see cref="DigestDeliveryStatus.Failed"/>.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Channel at attempt time.</summary>
    public string ChannelType { get; set; } = null!;

    /// <summary>Destination at attempt time.</summary>
    public string Destination { get; set; } = null!;
}
