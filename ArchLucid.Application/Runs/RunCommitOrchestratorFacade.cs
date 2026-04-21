using ArchLucid.Application.Runs.Orchestration;

namespace ArchLucid.Application.Runs;

/// <inheritdoc cref="IRunCommitOrchestrator"/>
public sealed class RunCommitOrchestratorFacade(IArchitectureRunCommitOrchestrator inner) : IRunCommitOrchestrator
{
    private readonly IArchitectureRunCommitOrchestrator _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public Task<CommitRunResult> CommitRunAsync(string runId, CancellationToken cancellationToken = default) =>
        _inner.CommitRunAsync(runId, cancellationToken);
}
