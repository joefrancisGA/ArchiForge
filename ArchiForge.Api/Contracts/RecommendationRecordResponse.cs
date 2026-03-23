namespace ArchiForge.Api.Contracts;

/// <summary>
/// API projection of a persisted <c>RecommendationRecord</c> for <c>GET</c> / <c>POST …/action</c> advisory endpoints (excludes supporting-id JSON blobs).
/// </summary>
/// <remarks>
/// Mapped from <see cref="ArchiForge.Decisioning.Advisory.Workflow.RecommendationRecord"/> in <see cref="ArchiForge.Api.Controllers.AdvisoryController"/>.
/// </remarks>
public sealed class RecommendationRecordResponse
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

    public string Status { get; set; } = null!;

    public DateTime CreatedUtc
    {
        get; set;
    }
    public DateTime LastUpdatedUtc
    {
        get; set;
    }

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
}
