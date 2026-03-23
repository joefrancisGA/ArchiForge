namespace ArchiForge.Decisioning.Alerts.Delivery;

/// <summary>
/// Audit row for a single delivery try: which alert, which subscription, channel snapshot, and outcome.
/// </summary>
public class AlertDeliveryAttempt
{
    /// <summary>Attempt primary key.</summary>
    public Guid AlertDeliveryAttemptId { get; set; } = Guid.NewGuid();

    /// <summary>Alert that was being delivered.</summary>
    public Guid AlertId
    {
        get; set;
    }

    /// <summary>Routing subscription used for this attempt.</summary>
    public Guid RoutingSubscriptionId
    {
        get; set;
    }

    /// <summary>Scope copied from the alert for querying.</summary>
    public Guid TenantId
    {
        get; set;
    }

    /// <summary>Scope copied from the alert.</summary>
    public Guid WorkspaceId
    {
        get; set;
    }

    /// <summary>Scope copied from the alert.</summary>
    public Guid ProjectId
    {
        get; set;
    }

    /// <summary>When the attempt started (UTC).</summary>
    public DateTime AttemptedUtc { get; set; } = DateTime.UtcNow;

    /// <summary><see cref="AlertDeliveryAttemptStatus"/> value.</summary>
    public string Status { get; set; } = AlertDeliveryAttemptStatus.Started;

    /// <summary>Populated when <see cref="Status"/> is <see cref="AlertDeliveryAttemptStatus.Failed"/>.</summary>
    public string? ErrorMessage
    {
        get; set;
    }

    /// <summary>Channel at attempt time (denormalized).</summary>
    public string ChannelType { get; set; } = null!;

    /// <summary>Destination at attempt time.</summary>
    public string Destination { get; set; } = null!;

    /// <summary>Reserved for retry policies; dispatcher currently writes <c>0</c>.</summary>
    public int RetryCount
    {
        get; set;
    }
}
