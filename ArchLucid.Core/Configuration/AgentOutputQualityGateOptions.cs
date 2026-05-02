namespace ArchLucid.Core.Configuration;

/// <summary>
///     Optional post-evaluation gate on persisted agent JSON (structural + semantic scores). On by default with
///     conservative warn thresholds and reject floors at <c>0</c> (warn-only V1); set <see cref="Enabled" /> to false
///     to disable.
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
    } = 0.3;

    /// <summary>Semantic score below this yields <c>warned</c> unless <see cref="SemanticRejectBelow" /> triggers first.</summary>
    public double SemanticWarnBelow
    {
        get;
        set;
    } = 0.2;

    /// <summary>
    ///     Structural ratio strictly below this yields <c>rejected</c>. Default <c>0</c> disables structural reject
    ///     (no positive score is strictly below zero).
    /// </summary>
    public double StructuralRejectBelow
    {
        get;
        set;
    }

    /// <summary>
    ///     Semantic score strictly below this yields <c>rejected</c>. Default <c>0</c> disables semantic reject for
    ///     non-negative scores.
    /// </summary>
    public double SemanticRejectBelow
    {
        get;
        set;
    }

    /// <summary>
    ///     When <c>true</c>, a <c>Rejected</c> outcome causes
    ///     <c>AgentOutputEvaluationRecorder.EvaluateAndRecordMetricsAsync</c> to throw
    ///     <see cref="AgentOutputQualityGateRejectedException" /> after emitting metrics and logs.
    ///     Defaults to <c>false</c> so existing behaviour (metrics-only) is preserved until a product
    ///     decision explicitly enables enforcement.
    /// </summary>
    public bool EnforceOnReject
    {
        get;
        set;
    } = false;
}
