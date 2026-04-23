namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Read-only snapshot of cumulative <see cref="ArchLucidInstrumentation" /> counters that are safe to surface
///     on a public-facing proof page (process-life cumulative; resets when the API host restarts).
/// </summary>
public sealed class InstrumentationCounterSnapshot
{
    /// <summary>Sum of <c>archlucid_runs_created_total</c> measurements observed since process start.</summary>
    public long RunsCreatedTotal
    {
        get;
        init;
    }

    /// <summary>Sum of <c>archlucid_findings_produced_total</c> measurements grouped by the <c>severity</c> tag.</summary>
    public IReadOnlyDictionary<string, long> FindingsProducedBySeverity
    {
        get;
        init;
    } =
        new Dictionary<string, long>(StringComparer.Ordinal);

    /// <summary>Sum of <c>archlucid_operator_task_success_total</c> grouped by the <c>task</c> tag (process lifetime).</summary>
    public IReadOnlyDictionary<string, long> OperatorTaskSuccessByTask
    {
        get;
        init;
    } =
        new Dictionary<string, long>(StringComparer.Ordinal);
}
