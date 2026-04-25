using ArchLucid.Decisioning.Advisory.Analysis;

namespace ArchLucid.Decisioning.Advisory.Models;

/// <summary>
///     Intermediate finding emitted by <see cref="IImprovementSignalAnalyzer" /> before scoring and folding into
///     <see cref="ImprovementRecommendation" /> entries.
/// </summary>
/// <remarks>
///     <see cref="SignalType" /> values include <c>UncoveredRequirement</c>, <c>SecurityGap</c>, <c>SecurityRegression</c>
///     , <c>CostIncrease</c>, etc.
/// </remarks>
public class ImprovementSignal
{
    /// <summary>Stable classifier for telemetry and grouping.</summary>
    public string SignalType
    {
        get;
        set;
    } = null!;

    /// <summary>Broad domain (e.g. Security, Compliance, Cost).</summary>
    public string Category
    {
        get;
        set;
    } = null!;

    /// <summary>Short headline.</summary>
    public string Title
    {
        get;
        set;
    } = null!;

    /// <summary>Operator-facing detail.</summary>
    public string Description
    {
        get;
        set;
    } = null!;

    /// <summary>Rough severity (e.g. Low/Medium/High/Critical).</summary>
    public string Severity
    {
        get;
        set;
    } = "Medium";

    /// <summary>Linked finding ids from the manifest or snapshot when available.</summary>
    public List<string> FindingIds
    {
        get;
        set;
    } = [];

    /// <summary>Linked decision keys when applicable.</summary>
    public List<string> DecisionIds
    {
        get;
        set;
    } = [];
}
