namespace ArchLucid.Application.Runs.Finalization;

/// <summary>
///     Atomically persists authority decision trace + golden manifest and finalizes the run header, durable audit, and
///     integration outbox (SQL path), or best-effort equivalent when the unit of work has no shared transaction.
/// </summary>
public interface IManifestFinalizationService
{
    Task<ManifestFinalizationResult> FinalizeAsync(
        ManifestFinalizationRequest request,
        CancellationToken cancellationToken = default);
}
