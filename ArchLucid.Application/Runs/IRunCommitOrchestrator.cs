namespace ArchLucid.Application.Runs;

/// <summary>
/// Write-side façade for manifest commit (ADR 0021 Phase 3 preparation). Today delegates to
/// <see cref="Orchestration.IArchitectureRunCommitOrchestrator"/>; callers that should stay pipeline-neutral
/// depend on this contract instead of the concrete coordinator orchestrator type.
/// </summary>
public interface IRunCommitOrchestrator
{
    /// <summary>Merges persisted agent outputs into a golden manifest and completes the commit phase.</summary>
    Task<CommitRunResult> CommitRunAsync(string runId, CancellationToken cancellationToken = default);
}
