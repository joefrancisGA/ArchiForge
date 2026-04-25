namespace ArchLucid.Decisioning.Advisory.Models;

/// <summary>
///     Canonical <see cref="ImprovementSignal.Category" /> values used by
///     <see cref="Analysis.ImprovementSignalAnalyzer" /> and <see cref="Services.RecommendationGenerator" />.
/// </summary>
public static class ImprovementSignalCategories
{
    public const string Requirement = "Requirement";
    public const string Security = "Security";
    public const string Compliance = "Compliance";
    public const string Topology = "Topology";
    public const string Cost = "Cost";
    public const string Risk = "Risk";
}
