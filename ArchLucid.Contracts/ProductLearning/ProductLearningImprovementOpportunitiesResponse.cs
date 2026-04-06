namespace ArchiForge.Contracts.ProductLearning;

/// <summary>Ranked improvement opportunities for the current scope and query window.</summary>
public sealed class ProductLearningImprovementOpportunitiesResponse
{
    public DateTime GeneratedUtc { get; init; }
    public IReadOnlyList<ImprovementOpportunity> Opportunities { get; init; } = [];
}
