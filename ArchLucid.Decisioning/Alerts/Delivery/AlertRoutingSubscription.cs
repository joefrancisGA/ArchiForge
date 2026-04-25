namespace ArchLucid.Decisioning.Alerts.Delivery;

/// <summary>
///     Configures one outbound path for alerts: channel type, destination, minimum severity, and enablement for a scope.
/// </summary>
/// <remarks>
///     <see cref="ChannelType" /> should align with <see cref="AlertRoutingChannelType" /> and a registered
///     <see cref="IAlertDeliveryChannel" />.
/// </remarks>
public class AlertRoutingSubscription
{
    /// <summary>Primary key; assigned by API on create.</summary>
    public Guid RoutingSubscriptionId
    {
        get;
        set;
    } = Guid.NewGuid();

    /// <summary>Tenant owning this route.</summary>
    public Guid TenantId
    {
        get;
        set;
    }

    /// <summary>Workspace within the tenant.</summary>
    public Guid WorkspaceId
    {
        get;
        set;
    }

    /// <summary>Project within the workspace.</summary>
    public Guid ProjectId
    {
        get;
        set;
    }

    /// <summary>Operator-facing label.</summary>
    public string Name
    {
        get;
        set;
    } = "Alert Routing Subscription";

    /// <summary>E.g. <see cref="AlertRoutingChannelType.Email" />.</summary>
    public string ChannelType
    {
        get;
        set;
    } = null!;

    /// <summary>Email address, webhook URL, or channel-specific target.</summary>
    public string Destination
    {
        get;
        set;
    } = null!;

    /// <summary>Alerts below this severity (per <see cref="AlertSeverityComparer" />) are not sent on this route.</summary>
    public string MinimumSeverity
    {
        get;
        set;
    } = AlertSeverity.Warning;

    /// <summary>When false, excluded from <see cref="IAlertRoutingSubscriptionRepository.ListEnabledByScopeAsync" />.</summary>
    public bool IsEnabled
    {
        get;
        set;
    } = true;

    /// <summary>Row creation time (UTC).</summary>
    public DateTime CreatedUtc
    {
        get;
        set;
    } = DateTime.UtcNow;

    /// <summary>Last successful delivery timestamp; updated by the dispatcher.</summary>
    public DateTime? LastDeliveredUtc
    {
        get;
        set;
    }

    /// <summary>Opaque JSON for future channel options.</summary>
    public string MetadataJson
    {
        get;
        set;
    } = "{}";
}
