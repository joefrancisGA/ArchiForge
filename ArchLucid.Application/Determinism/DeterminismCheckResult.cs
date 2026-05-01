namespace ArchLucid.Application.Determinism;

/// <summary>
///     Outcome of a determinism check run by <see cref="IDeterminismCheckService" />.
/// </summary>
public sealed class DeterminismCheckResult
{
    /// <summary>The original run identifier that was replayed.</summary>
    public string SourceRunId
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Number of replay iterations that were executed (matches <see cref="DeterminismCheckRequest.Iterations" />).</summary>
    public int Iterations
    {
        get;
        set;
    }

    /// <summary>Agent execution mode used for all replays (e.g. <c>"Current"</c>).</summary>
    public string ExecutionMode
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    ///     <c>true</c> when every iteration matched the baseline on both agent results and manifest output.
    /// </summary>
    public bool IsDeterministic
    {
        get;
        set;
    }

    /// <summary>Run identifier of the baseline replay (first replay, used as the reference for all comparisons).</summary>
    public string BaselineReplayRunId
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Per-iteration comparison results.</summary>
    public List<DeterminismIterationResult> IterationResults
    {
        get;
        set;
    } = [];

    /// <summary>Non-fatal warnings (e.g. overall drift summary). Empty when the run is deterministic.</summary>
    public List<string> Warnings
    {
        get;
        set;
    } = [];
}
