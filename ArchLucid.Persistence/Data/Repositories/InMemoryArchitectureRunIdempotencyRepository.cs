using System.Data;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Thread-safe in-memory <see cref="IArchitectureRunIdempotencyRepository" /> for tests and local scenarios.
/// </summary>
public sealed class InMemoryArchitectureRunIdempotencyRepository : IArchitectureRunIdempotencyRepository
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, ArchitectureRunIdempotencyLookup> _rows = new();

    /// <inheritdoc />
    public Task<ArchitectureRunIdempotencyLookup?> TryGetAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        byte[] idempotencyKeyHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(idempotencyKeyHash);
        cancellationToken.ThrowIfCancellationRequested();
        string k = Key(tenantId, workspaceId, projectId, idempotencyKeyHash);
        lock (_gate)

            return Task.FromResult(_rows.TryGetValue(k, out ArchitectureRunIdempotencyLookup? v) ? v : null);
    }

    /// <inheritdoc />
    public Task<bool> TryInsertAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        byte[] idempotencyKeyHash,
        byte[] requestFingerprint,
        string runId,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(idempotencyKeyHash);
        ArgumentNullException.ThrowIfNull(requestFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        cancellationToken.ThrowIfCancellationRequested();
        string k = Key(tenantId, workspaceId, projectId, idempotencyKeyHash);
        lock (_gate)
        {
            if (_rows.ContainsKey(k))
                return Task.FromResult(false);


            _rows[k] = new ArchitectureRunIdempotencyLookup
            {
                RunId = runId, RequestFingerprint = (byte[])requestFingerprint.Clone()
            };

            return Task.FromResult(true);
        }
    }

    private static string Key(Guid tenantId, Guid workspaceId, Guid projectId, byte[] idempotencyKeyHash)
    {
        return $"{tenantId:N}|{workspaceId:N}|{projectId:N}|{Convert.ToHexString(idempotencyKeyHash)}";
    }
}
