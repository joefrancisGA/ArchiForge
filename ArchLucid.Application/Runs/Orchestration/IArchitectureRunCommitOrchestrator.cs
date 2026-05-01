namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
///     Merges agent outputs into a golden manifest and persists commit artifacts (commit phase).
/// </summary>
public interface IArchitectureRunCommitOrchestrator
{
    Task<CommitRunResult> CommitRunAsync(string runId, CancellationToken cancellationToken = default);
}
