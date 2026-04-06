namespace ArchiForge.Application.Determinism;

/// <summary>
/// Parameters for a determinism check executed by <see cref="IDeterminismCheckService"/>.
/// </summary>
public sealed class DeterminismCheckRequest
{
    /// <summary>Run identifier to replay. Must not be blank.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>
    /// Number of replay iterations to perform. Must be at least 2 (baseline + one comparison).
    /// Defaults to <c>5</c>.
    /// </summary>
    public int Iterations { get; set; } = 5;

    /// <summary>
    /// Agent execution mode passed to <see cref="ArchiForge.Application.Agents.IAgentExecutorResolver"/>
    /// (e.g. <c>"Current"</c>). Defaults to <c>"Current"</c>.
    /// </summary>
    public string ExecutionMode { get; set; } = "Current";

    /// <summary>
    /// When <c>true</c>, each replay run is committed with a determinism-prefixed manifest version
    /// so the results are persisted and can be inspected after the check. Defaults to <c>false</c>.
    /// </summary>
    public bool CommitReplays { get; set; } = false;
}
