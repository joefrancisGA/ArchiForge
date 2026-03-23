namespace ArchiForge.Decisioning.Advisory.Workflow;

/// <summary>
/// Durable advisory recommendation row: scope, run linkage, workflow status, reviewer fields, and JSON arrays of supporting entity ids.
/// </summary>
/// <remarks>
/// Maps to <c>dbo.RecommendationRecords</c>. Status values are defined on <see cref="RecommendationStatus"/>.
/// <see cref="SupportingFindingIdsJson"/>, <see cref="SupportingDecisionIdsJson"/>, and <see cref="SupportingArtifactIdsJson"/> are stored as JSON text (camelCase object arrays in typical writes).
/// </remarks>
public class RecommendationRecord
{
    public Guid RecommendationId
    {
        get; set;
    }

    public Guid TenantId
    {
        get; set;
    }
    public Guid WorkspaceId
    {
        get; set;
    }
    public Guid ProjectId
    {
        get; set;
    }

    public Guid RunId
    {
        get; set;
    }
    public Guid? ComparedToRunId
    {
        get; set;
    }

    public string Title { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Rationale { get; set; } = null!;
    public string SuggestedAction { get; set; } = null!;
    public string Urgency { get; set; } = null!;
    public string ExpectedImpact { get; set; } = null!;
    public int PriorityScore
    {
        get; set;
    }

    public string Status { get; set; } = RecommendationStatus.Proposed;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    public string? ReviewedByUserId
    {
        get; set;
    }
    public string? ReviewedByUserName
    {
        get; set;
    }
    public string? ReviewComment
    {
        get; set;
    }
    public string? ResolutionRationale
    {
        get; set;
    }

    public string SupportingFindingIdsJson { get; set; } = "[]";
    public string SupportingDecisionIdsJson { get; set; } = "[]";
    public string SupportingArtifactIdsJson { get; set; } = "[]";
}
