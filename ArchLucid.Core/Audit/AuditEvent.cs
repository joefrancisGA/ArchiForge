namespace ArchLucid.Core.Audit;

/// <summary>
///     Represents a single auditable action taken within the system.
///     Audit events are append-only; they should never be modified after creation.
/// </summary>
public class AuditEvent
{
    /// <summary>Unique identifier for this audit event. Defaults to a new <see cref="Guid" />.</summary>
    public Guid EventId
    {
        get;
        set;
    } = Guid.NewGuid();

    /// <summary>UTC timestamp at which the event occurred.</summary>
    public DateTime OccurredUtc
    {
        get;
        set;
    } = DateTime.UtcNow;

    /// <summary>
    ///     A domain-specific event type string (e.g. <c>"RunCreated"</c>, <c>"ManifestPromoted"</c>).
    ///     Consumers should use constants from <c>GovernanceAuditEventTypes</c> or equivalent
    ///     event-type registries rather than free-form strings.
    /// </summary>
    public string EventType
    {
        get;
        set;
    } = null!;

    /// <summary>Internal user identifier of the actor who triggered the event.</summary>
    public string ActorUserId
    {
        get;
        set;
    } = null!;

    /// <summary>Display name of the actor for human-readable audit views.</summary>
    public string ActorUserName
    {
        get;
        set;
    } = null!;

    /// <summary>Tenant scope of the event.</summary>
    public Guid TenantId
    {
        get;
        set;
    }

    /// <summary>Workspace scope of the event.</summary>
    public Guid WorkspaceId
    {
        get;
        set;
    }

    /// <summary>Project scope of the event.</summary>
    public Guid ProjectId
    {
        get;
        set;
    }

    /// <summary>Optional run associated with the event. <see langword="null" /> for non-run events.</summary>
    public Guid? RunId
    {
        get;
        set;
    }

    /// <summary>Optional manifest associated with the event. <see langword="null" /> when not applicable.</summary>
    public Guid? ManifestId
    {
        get;
        set;
    }

    /// <summary>Optional artifact associated with the event. <see langword="null" /> when not applicable.</summary>
    public Guid? ArtifactId
    {
        get;
        set;
    }

    /// <summary>
    ///     Additional event-specific payload serialised as JSON. Defaults to <c>"{}"</c>.
    ///     The schema is event-type specific; callers should document the expected shape via
    ///     the corresponding event-type constant or handler.
    /// </summary>
    public string DataJson
    {
        get;
        set;
    } = "{}";

    /// <summary>
    ///     Optional correlation identifier linking related operations (e.g. an HTTP request trace ID).
    /// </summary>
    public string? CorrelationId
    {
        get;
        set;
    }
}
