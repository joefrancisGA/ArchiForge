namespace ArchiForge.Decisioning.Advisory.Learning;

/// <summary>
/// Aggregated recommendation outcomes and derived weights for a scope (used in advisory UX and alert metric snapshots).
/// </summary>
/// <remarks>
/// Produced by <see cref="IRecommendationLearningService.RebuildProfileAsync"/> and read by <c>GetLatestProfileAsync</c>.
/// </remarks>
public class RecommendationLearningProfile
{
    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    /// <summary>When this profile snapshot was generated (UTC).</summary>
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Counts by recommendation category.</summary>
    public List<RecommendationOutcomeStats> CategoryStats { get; set; } = [];

    /// <summary>Counts by urgency band.</summary>
    public List<RecommendationOutcomeStats> UrgencyStats { get; set; } = [];

    /// <summary>Counts by signal/type facet.</summary>
    public List<RecommendationOutcomeStats> SignalTypeStats { get; set; } = [];

    /// <summary>Optional weighting hints per category.</summary>
    public Dictionary<string, double> CategoryWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Optional weighting hints per urgency.</summary>
    public Dictionary<string, double> UrgencyWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Optional weighting hints per signal type.</summary>
    public Dictionary<string, double> SignalTypeWeights { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Analyzer notes and caveats.</summary>
    public List<string> Notes { get; set; } = [];
}
