namespace ArchLucid.Core.Audit;

/// <summary>Filters for scoped audit queries (defense-in-depth with tenant/workspace/project).</summary>
public sealed class AuditEventFilter
{
    public string? EventType { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    public string? CorrelationId { get; set; }

    public string? ActorUserId { get; set; }

    public Guid? RunId { get; set; }

    public int Take { get; set; } = 100;
}
