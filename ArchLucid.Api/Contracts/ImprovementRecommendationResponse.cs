using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON DTO for <see cref="ArchiForge.Decisioning.Advisory.Models.ImprovementRecommendation"/> (subset of fields exposed per API contract).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "API contract DTO; no business logic.")]
public class ImprovementRecommendationResponse
{
    /// <summary>Recommendation id from the domain model.</summary>
    public Guid RecommendationId { get; set; }

    /// <inheritdoc cref="ArchiForge.Decisioning.Advisory.Models.ImprovementRecommendation.Title"/>
    public string Title { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Decisioning.Advisory.Models.ImprovementRecommendation.Category"/>
    public string Category { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Decisioning.Advisory.Models.ImprovementRecommendation.Rationale"/>
    public string Rationale { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Decisioning.Advisory.Models.ImprovementRecommendation.SuggestedAction"/>
    public string SuggestedAction { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Decisioning.Advisory.Models.ImprovementRecommendation.Urgency"/>
    public string Urgency { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Decisioning.Advisory.Models.ImprovementRecommendation.ExpectedImpact"/>
    public string ExpectedImpact { get; set; } = null!;

    /// <inheritdoc cref="ArchiForge.Decisioning.Advisory.Models.ImprovementRecommendation.PriorityScore"/>
    public int PriorityScore { get; set; }
}
