namespace ArchLucid.Core.Configuration;

/// <summary>
///     Optional post-evaluation gate on persisted agent JSON (structural + semantic scores). On by default; set
///     <see cref="Enabled" /> to false to disable.
/// </summary>
public sealed class AgentOutputQualityGateOptions
{
    public const string SectionPath = "ArchLucid:AgentOutput:QualityGate";

    /// <summary>When false, the gate always accepts and emits no gate metrics.</summary>
    public bool Enabled
    {
        get;
        set;
    } = true;

    /// <summary>Structural ratio below this yields <c>warned</c> unless <see cref="StructuralRejectBelow" /> triggers first.</summary>
    public double StructuralWarnBelow
    {
        get;
        set;
    } = 0.55;

    /// <summary>Semantic score below this yields <c>warned</c> unless <see cref="SemanticRejectBelow" /> triggers first.</summary>
    public double SemanticWarnBelow
    {
        get;
        set;
    } = 0.55;

    /// <summary>Structural ratio strictly below this yields <c>rejected</c>.</summary>
    public double StructuralRejectBelow
    {
        get;
        set;
    } = 0.35;

    /// <summary>Semantic score strictly below this yields <c>rejected</c>.</summary>
    public double SemanticRejectBelow
    {
        get;
        set;
    } = 0.35;
}
