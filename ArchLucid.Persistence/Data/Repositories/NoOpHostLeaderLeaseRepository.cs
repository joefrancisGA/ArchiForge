namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Always succeeds; used when host leader election is disabled in configuration or storage is InMemory (no SQL leases).
/// </summary>
public sealed class NoOpHostLeaderLeaseRepository : IHostLeaderLeaseRepository
{
    /// <inheritdoc />
    public Task<bool> TryAcquireOrRenewAsync(
        string leaseName,
        string instanceId,
        int leaseDurationSeconds,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task TryReleaseAsync(string leaseName, string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<HostLeaderLeaseSnapshot>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<HostLeaderLeaseSnapshot>>([]);
    }
}
