using ArchiForge.Persistence.Data.Repositories;
using ArchiForge.Persistence.Integration;

namespace ArchiForge.Api.Services.Admin;

/// <summary>Aggregates SQL-backed operational counters for <see cref="Controllers.AdminController"/>.</summary>
public interface IAdminDiagnosticsService
{
    /// <summary>Pending rows in authority pipeline work and retrieval indexing outboxes.</summary>
    Task<AdminOutboxSnapshot> GetOutboxSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Current host leader lease rows.</summary>
    Task<IReadOnlyList<HostLeaderLeaseSnapshot>> GetLeasesAsync(CancellationToken cancellationToken = default);

    /// <summary>Recent integration event outbox dead letters (Service Bus publish failures).</summary>
    Task<IReadOnlyList<IntegrationEventOutboxDeadLetterRow>> ListIntegrationOutboxDeadLettersAsync(
        int maxRows,
        CancellationToken cancellationToken = default);

    /// <summary>Re-queues a dead-letter row for another publish attempt cycle.</summary>
    Task<bool> RetryIntegrationOutboxDeadLetterAsync(Guid outboxId, CancellationToken cancellationToken = default);
}
