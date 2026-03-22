namespace ArchiForge.Api.Contracts;

public sealed class RecommendationRecordResponse
{
    public Guid RecommendationId { get; set; }

    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid RunId { get; set; }
    public Guid? ComparedToRunId { get; set; }

    public string Title { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Rationale { get; set; } = default!;
    public string SuggestedAction { get; set; } = default!;
    public string Urgency { get; set; } = default!;
    public string ExpectedImpact { get; set; } = default!;
    public int PriorityScore { get; set; }

    public string Status { get; set; } = default!;

    public DateTime CreatedUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }

    public string? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public string? ReviewComment { get; set; }
    public string? ResolutionRationale { get; set; }
}
