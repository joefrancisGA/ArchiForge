namespace ArchLucid.Core.Configuration;

/// <summary>Optional hour multipliers for <c>GET /v1/architecture/run/{runId}/roi</c> (directional estimates).</summary>
public sealed class RunRoiEstimatorOptions
{
    public const string SectionPath = "Architecture:RunRoiEstimator";

    /// <summary>Estimated analyst hours saved per architecture finding aggregated across agent results.</summary>
    public double HoursPerArchitectureFinding
    {
        get;
        init;
    } = 2;

    /// <summary>Hours credited per modeled manifest element (services + datastores + relationships).</summary>
    public double HoursPerManifestModeledElement
    {
        get;
        init;
    } = 0.08;

    /// <summary>Hours credited per decision trace emitted at commit.</summary>
    public double HoursPerDecisionTrace
    {
        get;
        init;
    } = 0.25;

    /// <summary>Hours credited per completed agent result row.</summary>
    public double HoursPerCompletedAgentResult
    {
        get;
        init;
    } = 0.5;
}
