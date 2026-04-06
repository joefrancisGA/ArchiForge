namespace ArchiForge.Contracts.ProductLearning;

/// <summary>
/// A product issue or enhancement candidate inferred from feedback patterns (for triage — not an auto-commit backlog item).
/// </summary>
public sealed class ImprovementOpportunity
{
    /// <summary>Correlation id for UI, exports, and future persistence of opportunities.</summary>
    public Guid OpportunityId { get; init; }

    /// <summary>Optional link to the rollup key that produced this opportunity.</summary>
    public string? SourceAggregateKey { get; init; }
    public string? PatternKey { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;

    /// <summary>Where in the product/output lifecycle the pain shows up.</summary>
    public string AffectedArtifactTypeOrWorkflowArea { get; init; } = string.Empty;

    /// <summary>Severity band (convention defined by operators; e.g. Low/Medium/High).</summary>
    public string Severity { get; init; } = string.Empty;

    /// <summary>Lower values indicate higher urgency for product review (1 = first).</summary>
    public int PriorityRank { get; init; }

    /// <summary>Optional routing hint (e.g. Product, Architecture, Agent pipeline).</summary>
    public string? SuggestedOwnerRole { get; init; }
    public int EvidenceSignalCount { get; init; }
    public int DistinctRunCount { get; init; }
    public double? AverageTrustScore { get; init; }

    /// <summary>Short excerpt evidencing repetition (e.g. common comment fragment).</summary>
    public string? RepeatedThemeSnippet { get; init; }
    public DateTime FirstSeenUtc { get; init; }
    public DateTime LastSeenUtc { get; init; }
}
