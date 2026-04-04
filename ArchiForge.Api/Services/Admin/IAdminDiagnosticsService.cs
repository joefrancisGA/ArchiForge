using ArchiForge.Data.Repositories;

namespace ArchiForge.Api.Services.Admin;

/// <summary>Aggregates SQL-backed operational counters for <see cref="Controllers.AdminController"/>.</summary>
public interface IAdminDiagnosticsService
{
    /// <summary>Pending rows in authority pipeline work and retrieval indexing outboxes.</summary>
    Task<AdminOutboxSnapshot> GetOutboxSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Current host leader lease rows.</summary>
    Task<IReadOnlyList<HostLeaderLeaseSnapshot>> GetLeasesAsync(CancellationToken cancellationToken = default);
}
