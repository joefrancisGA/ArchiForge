namespace ArchLucid.Core.Audit;

/// <summary>Filters for scoped audit queries (defense-in-depth with tenant/workspace/project).</summary>
public sealed class AuditEventFilter
{
    public string? EventType
    {
        get;
        set;
    }

    public DateTime? FromUtc
    {
        get;
        set;
    }

    public DateTime? ToUtc
    {
        get;
        set;
    }

    /// <summary>Cursor: return only events with OccurredUtc strictly before this value. Enables keyset pagination.</summary>
    public DateTime? BeforeUtc
    {
        get;
        set;
    }

    /// <summary>
    ///     Keyset tie-break when multiple events share the same <see cref="BeforeUtc" /> instant: return only rows with
    ///     <c>EventId</c> strictly less than this value (same sort order as SQL: <c>OccurredUtc DESC, EventId DESC</c>).
    ///     Omit unless <see cref="BeforeUtc" /> is set.
    /// </summary>
    public Guid? BeforeEventId
    {
        get;
        set;
    }

    public string? CorrelationId
    {
        get;
        set;
    }

    public string? ActorUserId
    {
        get;
        set;
    }

    public Guid? RunId
    {
        get;
        set;
    }

    public int Take
    {
        get;
        set;
    } = 100;
}
