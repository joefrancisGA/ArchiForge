namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// Lightweight KPI + notes for the product-learning dashboard header (no large aggregate payloads).
/// </summary>
public sealed class ProductLearningDashboardSummaryResponse
{
    public DateTime GeneratedUtc { get; init; }
    public Guid TenantId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid ProjectId { get; init; }
    public int TotalSignalsInScope { get; init; }
    public int DistinctRunsTouched { get; init; }

    /// <summary>Count of rollups returned when a full dashboard is built with default triage options (informational for UI).</summary>
    public int TopAggregateCount { get; init; }
    public int ArtifactTrendCount { get; init; }
    public int ImprovementOpportunityCount { get; init; }
    public int TriageQueueItemCount { get; init; }
    public IReadOnlyList<string> SummaryNotes { get; init; } = [];
}
