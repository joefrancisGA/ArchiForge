namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// One item the product owner or architect should review next (queue semantics supplied by services later).
/// </summary>
public sealed class TriageQueueItem
{
    public Guid QueueItemId { get; init; }
    public Guid? RelatedSignalId { get; init; }
    public Guid? RelatedOpportunityId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string DetailSummary { get; init; } = string.Empty;
    public int PriorityRank { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string AffectedArtifactTypeOrWorkflowArea { get; init; } = string.Empty;

    /// <summary>Aligns with <see cref="ProductLearningTriageStatusValues"/> when sourced from signals.</summary>
    public string TriageStatus { get; init; } = string.Empty;
    public DateTime FirstSeenUtc { get; init; }
    public DateTime LastSeenUtc { get; init; }

    /// <summary>Optional one-line hint for the reviewer (no automation enforced).</summary>
    public string? SuggestedNextAction { get; init; }
}
