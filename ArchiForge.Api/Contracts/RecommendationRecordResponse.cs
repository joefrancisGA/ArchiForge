namespace ArchiForge.Api.Contracts;

public sealed class RecommendationRecordResponse
{
    public Guid RecommendationId { get; set; }

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid RunId { get; set; }
    public Guid? ComparedToRunId { get; set; }

    public string Title { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Rationale { get; set; } = null!;
    public string SuggestedAction { get; set; } = null!;
    public string Urgency { get; set; } = null!;
    public string ExpectedImpact { get; set; } = null!;
    public int PriorityScore { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }

    public string? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public string? ReviewComment { get; set; }
    public string? ResolutionRationale { get; set; }
}
