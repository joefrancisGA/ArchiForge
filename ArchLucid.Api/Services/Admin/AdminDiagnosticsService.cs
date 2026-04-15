using System.Data.Common;

using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.DataConsistency;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Coordination.Retrieval;
using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Orchestration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Services.Admin;

/// <inheritdoc cref="IAdminDiagnosticsService" />
public sealed class AdminDiagnosticsService(
    IAuthorityPipelineWorkRepository authorityPipelineWork,
    IRetrievalIndexingOutboxRepository retrievalIndexingOutbox,
    IIntegrationEventOutboxRepository integrationEventOutbox,
    IHostLeaderLeaseRepository hostLeaderLeases,
    IRunRepository runRepository,
    IDbConnectionFactory connectionFactory,
    IOptions<ArchLucidOptions> archLucidOptions) : IAdminDiagnosticsService
{
    private readonly IAuthorityPipelineWorkRepository _authorityPipelineWork =
        authorityPipelineWork ?? throw new ArgumentNullException(nameof(authorityPipelineWork));

    private readonly IRetrievalIndexingOutboxRepository _retrievalIndexingOutbox =
        retrievalIndexingOutbox ?? throw new ArgumentNullException(nameof(retrievalIndexingOutbox));

    private readonly IIntegrationEventOutboxRepository _integrationEventOutbox =
        integrationEventOutbox ?? throw new ArgumentNullException(nameof(integrationEventOutbox));

    private readonly IHostLeaderLeaseRepository _hostLeaderLeases =
        hostLeaderLeases ?? throw new ArgumentNullException(nameof(hostLeaderLeases));

    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    private readonly IDbConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private readonly IOptions<ArchLucidOptions> _archLucidOptions =
        archLucidOptions ?? throw new ArgumentNullException(nameof(archLucidOptions));

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

    /// <inheritdoc />
    public async Task<DataConsistencyOrphanCounts> GetDataConsistencyOrphanCountsAsync(
        CancellationToken cancellationToken = default)
    {
        if (ArchLucidOptions.EffectiveIsInMemory(_archLucidOptions.Value.StorageProvider))
        {
            return new DataConsistencyOrphanCounts(0, 0, 0, 0);
        }

        DbConnection connection = (DbConnection)_connectionFactory.CreateConnection();
        await using DbConnection _ = connection;
        await connection.OpenAsync(cancellationToken);

        long left = await ExecuteCountAsync(connection, DataConsistencyOrphanProbeSql.ComparisonRecordsLeftRunId, cancellationToken);
        long right = await ExecuteCountAsync(connection, DataConsistencyOrphanProbeSql.ComparisonRecordsRightRunId, cancellationToken);
        long golden = await ExecuteCountAsync(connection, DataConsistencyOrphanProbeSql.GoldenManifestsRunId, cancellationToken);
        long findings = await ExecuteCountAsync(connection, DataConsistencyOrphanProbeSql.FindingsSnapshotsRunId, cancellationToken);

        return new DataConsistencyOrphanCounts(left, right, golden, findings);
    }

    /// <inheritdoc />
    public Task<RunArchiveBatchResult> ArchiveRunsCreatedBeforeAsync(
        DateTimeOffset createdBeforeUtc,
        CancellationToken cancellationToken = default) =>
        _runRepository.ArchiveRunsCreatedBeforeAsync(createdBeforeUtc, cancellationToken);

    private static async Task<long> ExecuteCountAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is long l ? l : Convert.ToInt64(scalar ?? 0L, System.Globalization.CultureInfo.InvariantCulture);
    }
}
