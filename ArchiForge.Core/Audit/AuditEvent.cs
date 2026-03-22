namespace ArchiForge.Core.Audit;

public class AuditEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();

    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;

    public string EventType { get; set; } = null!;

    public string ActorUserId { get; set; } = null!;
    public string ActorUserName { get; set; } = null!;

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid? RunId { get; set; }
    public Guid? ManifestId { get; set; }
    public Guid? ArtifactId { get; set; }

    public string DataJson { get; set; } = "{}";

    public string? CorrelationId { get; set; }
}
