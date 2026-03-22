namespace ArchiForge.Decisioning.Alerts;

public class AlertRecord
{
    public Guid AlertId { get; set; } = Guid.NewGuid();
    public Guid RuleId { get; set; }

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid? RunId { get; set; }
    public Guid? ComparedToRunId { get; set; }
    public Guid? RecommendationId { get; set; }

    public string Title { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public string Status { get; set; } = AlertStatus.Open;

    public string TriggerValue { get; set; } = default!;
    public string Description { get; set; } = default!;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedUtc { get; set; }

    public string? AcknowledgedByUserId { get; set; }
    public string? AcknowledgedByUserName { get; set; }
    public string? ResolutionComment { get; set; }

    public string DeduplicationKey { get; set; } = default!;
}
