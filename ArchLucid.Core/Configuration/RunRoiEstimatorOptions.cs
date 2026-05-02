namespace ArchLucid.Core.Configuration;

/// <summary>Optional hour multipliers for <c>GET /v1/architecture/run/{runId}/roi</c> (directional estimates).</summary>
public sealed class RunRoiEstimatorOptions
{
    public const string SectionPath = "Architecture:RunRoiEstimator";

    /// <summary>Estimated analyst hours saved per architecture finding aggregated across agent results (legacy fallback).</summary>
    public double HoursPerArchitectureFinding
    {
        get;
        init;
    } = 2;

    /// <summary>Estimated analyst hours saved per Critical architecture finding.</summary>
    public double HoursPerCriticalFinding
    {
        get;
        init;
    } = 8;

    /// <summary>Estimated analyst hours saved per Error architecture finding.</summary>
    public double HoursPerErrorFinding
    {
        get;
        init;
    } = 4;

    /// <summary>Estimated analyst hours saved per Warning architecture finding.</summary>
    public double HoursPerWarningFinding
    {
        get;
        init;
    } = 2;

    /// <summary>Estimated analyst hours saved per Info architecture finding.</summary>
    public double HoursPerInfoFinding
    {
        get;
        init;
    } = 1;

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
