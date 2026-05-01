namespace ArchLucid.Application;

/// <summary>
///     Replays an existing architecture run by re-executing its agents against cloned evidence,
///     optionally committing the output as a new manifest version.
/// </summary>
public interface IReplayRunService
{
    /// <summary>
    ///     Creates a replay of <paramref name="originalRunId" />, running agents under
    ///     <paramref name="executionMode" /> and returning the produced results (and optionally manifest).
    /// </summary>
    /// <exception cref="RunNotFoundException">Thrown when the original run does not exist.</exception>
    Task<ReplayRunResult> ReplayAsync(
        string originalRunId,
        string executionMode = ExecutionModes.Current,
        bool commitReplay = false,
        string? manifestVersionOverride = null,
        CancellationToken cancellationToken = default);
}
