using ArchLucid.Application.Runs;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Application;

/// <summary>
/// Application-layer workflow for an architecture authority run: persist a new run after coordination, execute agent tasks, then merge and commit a golden manifest with decision traces.
/// </summary>
/// <remarks>
/// Implementation: <see cref="ArchitectureRunService"/> (scoped DI). HTTP surface: <c>ArchLucid.Api.Controllers.Authority.RunsController</c> (create via <c>architecture/request</c>, execute/commit via <c>architecture/run/{runId}/execute</c> and <c>architecture/run/{runId}/commit</c>).
/// </remarks>
public interface IArchitectureRunService
{
    /// <summary>
    /// Calls <see cref="ArchLucid.Coordinator.Services.ICoordinatorService.CreateRunAsync"/> and, on success, persists the request, run, evidence bundle, and starter tasks.
    /// </summary>
    /// <param name="request">Inbound architecture request.</param>
    /// <param name="idempotency">When non-<see langword="null"/>, deduplicates by HTTP scope + key hash; mismatched body → <see cref="ConflictException"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created run, bundle, and tasks.</returns>
    /// <exception cref="InvalidOperationException">Thrown when coordination fails validation (errors are aggregated into the message).</exception>
    /// <exception cref="ConflictException">Thrown when the same idempotency key was used with a different request fingerprint.</exception>
    Task<CreateRunResult> CreateRunAsync(
        ArchitectureRequest request,
        CreateRunIdempotencyState? idempotency = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds persisted evidence, runs <see cref="ArchLucid.Contracts.Abstractions.Agents.IAgentExecutor"/>, stores results and evaluations, and moves the run to <see cref="ArchLucid.Contracts.Common.ArchitectureRunStatus.ReadyForCommit"/>.
    /// </summary>
    /// <remarks>
    /// Idempotent when the run is already <see cref="ArchLucid.Contracts.Common.ArchitectureRunStatus.ReadyForCommit"/> or <see cref="ArchLucid.Contracts.Common.ArchitectureRunStatus.Committed"/> and agent results exist: returns stored results without re-executing agents.
    /// </remarks>
    /// <param name="runId">Run identifier (hex string without dashes, as returned from create).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Run id and agent results.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the run or request is missing, there are no tasks, or persistence is inconsistent.</exception>
    Task<ExecuteRunResult> ExecuteRunAsync(
        string runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves decision nodes (V2), merges via the decision engine into a <see cref="ArchLucid.Contracts.Manifest.GoldenManifest"/>, persists manifest and traces, and sets the run to <see cref="ArchLucid.Contracts.Common.ArchitectureRunStatus.Committed"/>.
    /// </summary>
    /// <remarks>
    /// Idempotent when the run is already committed and the manifest version can be loaded: returns the existing manifest and traces. On merge failure the run is marked <see cref="ArchLucid.Contracts.Common.ArchitectureRunStatus.Failed"/>.
    /// </remarks>
    /// <param name="runId">Run identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Manifest, decision traces, and merge warnings.</returns>
    /// <exception cref="ConflictException">Thrown when the run is in the wrong phase for commit (including failed runs).</exception>
    /// <exception cref="InvalidOperationException">Thrown when required data is missing or merge produces errors.</exception>
    Task<CommitRunResult> CommitRunAsync(
        string runId,
        CancellationToken cancellationToken = default);
}
