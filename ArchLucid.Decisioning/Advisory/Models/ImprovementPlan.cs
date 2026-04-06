using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Alerts;

namespace ArchiForge.Decisioning.Advisory.Models;

/// <summary>
/// Advisory output for a single run (and optional prior run): prioritized recommendations, narrative notes, and merged policy defaults for digest/alert context.
/// </summary>
/// <remarks>
/// Produced by <see cref="IImprovementAdvisorService"/> from golden manifest, findings, and optional <see cref="ArchiForge.Core.Comparison.ComparisonResult"/>.
/// Enriched during scheduled scans with <see cref="PolicyPackAdvisoryDefaults"/> before <see cref="AlertEvaluationContextFactory.ForAdvisoryScan"/>.
/// Serialized to clients as <c>ArchiForge.Api.Contracts.ImprovementPlanResponse</c>.
/// </remarks>
public class ImprovementPlan
{
    /// <summary>Run the plan describes.</summary>
    public Guid RunId { get; set; }

    /// <summary>Prior run used for comparison when present.</summary>
    public Guid? ComparedToRunId { get; set; }

    /// <summary>UTC timestamp when the plan was generated.</summary>
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Ordered advisory items (scores assigned by the advisor pipeline).</summary>
    public List<ImprovementRecommendation> Recommendations { get; set; } = [];

    /// <summary>High-level bullets for digests or UI.</summary>
    public List<string> SummaryNotes { get; set; } = [];

    /// <summary>Merged <c>advisoryDefaults</c> from effective policy packs (optional keys for advisory/digest tooling).</summary>
    public Dictionary<string, string> PolicyPackAdvisoryDefaults { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
