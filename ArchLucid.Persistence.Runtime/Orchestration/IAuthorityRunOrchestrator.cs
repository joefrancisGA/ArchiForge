using ArchiForge.ContextIngestion.Models;
using ArchiForge.Persistence.Models;

namespace ArchiForge.Persistence.Orchestration;

/// <summary>
/// End-to-end authority pipeline: context ingestion → graph → findings → decision engine → manifest → artifacts → (optional) retrieval indexing.
/// </summary>
/// <remarks>
/// Implementation: <see cref="AuthorityRunOrchestrator"/>. Primary caller: <c>ArchiForge.Coordinator.Services.CoordinatorService</c>. Registered scoped in API storage extensions.
/// Uses <see cref="ArchiForge.Persistence.Transactions.IArchiForgeUnitOfWork"/> when the factory supports transactional persistence; rolls back on failure.
/// </remarks>
public interface IAuthorityRunOrchestrator
{
    /// <summary>
    /// Creates a <see cref="RunRecord"/>, ingests context, builds snapshots, runs <see cref="Decisioning.Interfaces.IDecisionEngine.DecideAsync"/>, synthesizes artifacts, commits the unit of work, audits milestones, then best-effort semantic indexing.
    /// When the async authority feature is enabled, may persist only the run header and enqueue continuation work; <see cref="RunRecord.ContextSnapshotId"/> remains <see langword="null"/> until the worker completes <see cref="CompleteQueuedAuthorityPipelineAsync"/>.
    /// </summary>
    /// <param name="request">Ingestion payload; <see cref="ContextIngestion.Models.ContextIngestionRequest.RunId"/> is set to the new run id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="evidenceBundleIdForDeferredWork">When deferring, serialized into the work outbox so starter tasks reference the same evidence bundle id.</param>
    /// <returns>The persisted run with snapshot and manifest ids populated (or only <see cref="RunRecord.RunId"/> when deferred).</returns>
    Task<RunRecord> ExecuteAsync(
        ContextIngestionRequest request,
        CancellationToken cancellationToken = default,
        string? evidenceBundleIdForDeferredWork = null);

    /// <summary>
    /// Worker entry point: completes the pipeline for a run that was started with queued context/graph stages.
    /// </summary>
    Task<RunRecord> CompleteQueuedAuthorityPipelineAsync(ContextIngestionRequest request, CancellationToken cancellationToken = default);
}
