using ArchLucid.Persistence;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Api.Services.Admin;

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

    /// <summary>
    /// Counts rows referencing a missing authority <c>dbo.Runs</c> row (detection-only; same logic as the orphan probe).
    /// </summary>
    Task<DataConsistencyOrphanCounts> GetDataConsistencyOrphanCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Soft-archives runs with <c>CreatedUtc</c> strictly before the cutoff (see <see cref="IRunRepository.ArchiveRunsCreatedBeforeAsync"/>).</summary>
    Task<RunArchiveBatchResult> ArchiveRunsCreatedBeforeAsync(
        DateTimeOffset createdBeforeUtc,
        CancellationToken cancellationToken = default);
}
