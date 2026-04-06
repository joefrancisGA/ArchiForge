namespace ArchiForge.Decisioning.Advisory.Workflow;

/// <summary>
/// HTTP body for accept/reject/defer/implemented transitions on a <see cref="RecommendationRecord"/>.
/// </summary>
public class RecommendationActionRequest
{
    /// <summary>One of <see cref="RecommendationActionType"/> values (case-sensitive match in API validation).</summary>
    public string Action { get; set; } = null!;
    /// <summary>Optional operator comment stored on the recommendation row.</summary>
    public string? Comment { get; set; }
    /// <summary>Optional resolution rationale stored on <see cref="RecommendationRecord.ResolutionRationale"/>.</summary>
    public string? Rationale { get; set; }
}
