namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Persists <c>Idempotency-Key</c> → architecture run id mappings for <c>POST /architecture/request</c>.
/// </summary>
public interface IArchitectureRunIdempotencyRepository
{
    /// <summary>Returns the existing mapping when present.</summary>
    Task<ArchitectureRunIdempotencyLookup?> TryGetAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        byte[] idempotencyKeyHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new mapping. Returns <see langword="false"/> when another request committed the same key first (unique violation).
    /// </summary>
    Task<bool> TryInsertAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        byte[] idempotencyKeyHash,
        byte[] requestFingerprint,
        string runId,
        CancellationToken cancellationToken = default);
}
