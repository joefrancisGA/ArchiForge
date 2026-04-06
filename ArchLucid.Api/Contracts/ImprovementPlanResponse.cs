using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON shape for an advisory <see cref="ArchiForge.Decisioning.Advisory.Models.ImprovementPlan"/> returned from HTTP APIs.
/// </summary>
/// <remarks>Excludes <c>PolicyPackAdvisoryDefaults</c> when not needed on the wire for a given endpoint.</remarks>
[ExcludeFromCodeCoverage(Justification = "API contract DTO; no business logic.")]
public class ImprovementPlanResponse
{
    /// <summary>Run the plan describes.</summary>
    public Guid RunId { get; set; }

    /// <summary>Prior run id when a comparison was used.</summary>
    public Guid? ComparedToRunId { get; set; }

    /// <summary>UTC generation timestamp.</summary>
    public DateTime GeneratedUtc { get; set; }

    /// <summary>High-level notes.</summary>
    public List<string> SummaryNotes { get; set; } = [];

    /// <summary>Prioritized recommendations.</summary>
    public List<ImprovementRecommendationResponse> Recommendations { get; set; } = [];
}
