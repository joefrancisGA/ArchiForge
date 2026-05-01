namespace ArchLucid.Application;

/// <summary>
///     Well-known agent execution mode identifiers passed to
///     <see cref="ArchLucid.Application.Agents.IAgentExecutorResolver.Resolve" />.
/// </summary>
public static class ExecutionModes
{
    /// <summary>Run agents against the current live system state.</summary>
    public const string Current = "Current";

    /// <summary>Run agents in a fully deterministic, seed-controlled mode (used for determinism checks).</summary>
    public const string Deterministic = "Deterministic";

    /// <summary>Replay a prior run's inputs through agents without creating a new original run.</summary>
    public const string Replay = "Replay";
}
