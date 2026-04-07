using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Integration;
using ArchLucid.Persistence.Orchestration;
using ArchLucid.Persistence.Coordination.Retrieval;

namespace ArchLucid.Api.Services.Admin;

/// <inheritdoc cref="IAdminDiagnosticsService" />
public sealed class AdminDiagnosticsService(
    IAuthorityPipelineWorkRepository authorityPipelineWork,
    IRetrievalIndexingOutboxRepository retrievalIndexingOutbox,
    IIntegrationEventOutboxRepository integrationEventOutbox,
    IHostLeaderLeaseRepository hostLeaderLeases) : IAdminDiagnosticsService
{
    private readonly IAuthorityPipelineWorkRepository _authorityPipelineWork =
        authorityPipelineWork ?? throw new ArgumentNullException(nameof(authorityPipelineWork));

    private readonly IRetrievalIndexingOutboxRepository _retrievalIndexingOutbox =
        retrievalIndexingOutbox ?? throw new ArgumentNullException(nameof(retrievalIndexingOutbox));

    private readonly IIntegrationEventOutboxRepository _integrationEventOutbox =
        integrationEventOutbox ?? throw new ArgumentNullException(nameof(integrationEventOutbox));

    private readonly IHostLeaderLeaseRepository _hostLeaderLeases =
        hostLeaderLeases ?? throw new ArgumentNullException(nameof(hostLeaderLeases));

    /// <inheritdoc />
    public async Task<AdminOutboxSnapshot> GetOutboxSnapshotAsync(CancellationToken cancellationToken = default)
    {
        long authorityPending = await _authorityPipelineWork.CountPendingAsync(cancellationToken);
        long retrievalPending = await _retrievalIndexingOutbox.CountPendingAsync(cancellationToken);
        long integrationPending = await _integrationEventOutbox.CountIntegrationOutboxPublishPendingAsync(cancellationToken);
        long integrationDead = await _integrationEventOutbox.CountIntegrationOutboxDeadLetterAsync(cancellationToken);

        return new AdminOutboxSnapshot(authorityPending, retrievalPending, integrationPending, integrationDead);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<HostLeaderLeaseSnapshot>> GetLeasesAsync(CancellationToken cancellationToken = default) =>
        _hostLeaderLeases.ListAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<IntegrationEventOutboxDeadLetterRow>> ListIntegrationOutboxDeadLettersAsync(
        int maxRows,
        CancellationToken cancellationToken = default) =>
        _integrationEventOutbox.ListDeadLettersAsync(maxRows, cancellationToken);

    /// <inheritdoc />
    public Task<bool> RetryIntegrationOutboxDeadLetterAsync(Guid outboxId, CancellationToken cancellationToken = default) =>
        _integrationEventOutbox.ResetDeadLetterForRetryAsync(outboxId, cancellationToken);
}
