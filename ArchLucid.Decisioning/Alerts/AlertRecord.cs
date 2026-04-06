namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// Persisted alert instance: outcome of simple or composite evaluation, scoped to tenant/workspace/project.
/// </summary>
/// <remarks>
/// Stored in <c>dbo.AlertRecords</c>; listed via <c>AlertsController</c> and used in delivery payloads.
/// </remarks>
public class AlertRecord
{
    /// <summary>Primary key.</summary>
    public Guid AlertId { get; set; } = Guid.NewGuid();

    /// <summary>Originating simple rule id or composite rule id depending on <see cref="Category"/>.</summary>
    public Guid RuleId { get; set; }

    /// <summary>Tenant scope.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Workspace scope.</summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>Project scope.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Authority or advisory run when the alert was raised.</summary>
    public Guid? RunId { get; set; }

    /// <summary>Baseline run for comparison-driven alerts.</summary>
    public Guid? ComparedToRunId { get; set; }

    /// <summary>Optional link to a specific recommendation row.</summary>
    public Guid? RecommendationId { get; set; }

    /// <summary>Short headline for UI and notifications.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Domain grouping (e.g. rule type name or <c>CompositeAlert</c>).</summary>
    public string Category { get; set; } = null!;

    /// <summary>Typically <see cref="AlertSeverity"/>.</summary>
    public string Severity { get; set; } = null!;

    /// <summary>Lifecycle state (<see cref="AlertStatus"/>).</summary>
    public string Status { get; set; } = AlertStatus.Open;

    /// <summary>Compact metric or rule summary for triage.</summary>
    public string TriggerValue { get; set; } = null!;

    /// <summary>Longer explanation including metric context.</summary>
    public string Description { get; set; } = null!;

    /// <summary>Creation time (UTC).</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Updated on lifecycle actions.</summary>
    public DateTime? LastUpdatedUtc { get; set; }

    /// <summary>User id when acknowledged/resolved/suppressed.</summary>
    public string? AcknowledgedByUserId { get; set; }

    /// <summary>Display name for operator actions.</summary>
    public string? AcknowledgedByUserName { get; set; }

    /// <summary>Optional operator comment on resolve/suppress.</summary>
    public string? ResolutionComment { get; set; }

    /// <summary>Key used to suppress duplicate fires (simple and composite evaluators).</summary>
    public string DeduplicationKey { get; set; } = null!;
}
