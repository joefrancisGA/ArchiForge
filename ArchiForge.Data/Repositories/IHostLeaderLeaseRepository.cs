namespace ArchiForge.Data.Repositories;

/// <summary>
/// SQL-backed lease used so only one worker replica runs advisory polling, archival, and retrieval outbox loops.
/// </summary>
public interface IHostLeaderLeaseRepository
{
    /// <summary>
    /// Within a transaction with <c>UPDLOCK</c>, takes or renews the lease when it is expired or already held by <paramref name="instanceId"/>.
    /// </summary>
    /// <returns>True when this instance is the leader until <paramref name="leaseDurationSeconds"/> from now.</returns>
    Task<bool> TryAcquireOrRenewAsync(
        string leaseName,
        string instanceId,
        int leaseDurationSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort release so another replica can acquire without waiting for expiry.
    /// </summary>
    Task TryReleaseAsync(string leaseName, string instanceId, CancellationToken cancellationToken = default);

    /// <summary>Returns all lease rows (admin / diagnostics).</summary>
    Task<IReadOnlyList<HostLeaderLeaseSnapshot>> ListAllAsync(CancellationToken cancellationToken = default);
}
