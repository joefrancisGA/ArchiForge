using System.Data.Common;
using System.Globalization;
using System.Text.Json;

using ArchLucid.Application.Common;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Pagination;
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
    IOptions<ArchLucidOptions> archLucidOptions,
    IActorContext actorContext,
    IAuditService auditService) : IAdminDiagnosticsService
{
    private readonly IOptions<ArchLucidOptions> _archLucidOptions =
        archLucidOptions ?? throw new ArgumentNullException(nameof(archLucidOptions));

    private readonly IActorContext _actorContext =
        actorContext ?? throw new ArgumentNullException(nameof(actorContext));

    private readonly IAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IAuthorityPipelineWorkRepository _authorityPipelineWork =
        authorityPipelineWork ?? throw new ArgumentNullException(nameof(authorityPipelineWork));

    private readonly IDbConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private readonly IHostLeaderLeaseRepository _hostLeaderLeases =
        hostLeaderLeases ?? throw new ArgumentNullException(nameof(hostLeaderLeases));

    private readonly IIntegrationEventOutboxRepository _integrationEventOutbox =
        integrationEventOutbox ?? throw new ArgumentNullException(nameof(integrationEventOutbox));

    private readonly IRetrievalIndexingOutboxRepository _retrievalIndexingOutbox =
        retrievalIndexingOutbox ?? throw new ArgumentNullException(nameof(retrievalIndexingOutbox));

    private readonly IRunRepository _runRepository =
        runRepository ?? throw new ArgumentNullException(nameof(runRepository));

    /// <inheritdoc />
    public async Task<AdminOutboxSnapshot> GetOutboxSnapshotAsync(CancellationToken cancellationToken = default)
    {
        long authorityPending = await _authorityPipelineWork.CountPendingAsync(cancellationToken);
        long retrievalPending = await _retrievalIndexingOutbox.CountPendingAsync(cancellationToken);
        long integrationPending =
            await _integrationEventOutbox.CountIntegrationOutboxPublishPendingAsync(cancellationToken);
        long integrationDead = await _integrationEventOutbox.CountIntegrationOutboxDeadLetterAsync(cancellationToken);

        return new AdminOutboxSnapshot(authorityPending, retrievalPending, integrationPending, integrationDead);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<HostLeaderLeaseSnapshot>> GetLeasesAsync(CancellationToken cancellationToken = default)
    {
        return _hostLeaderLeases.ListAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IntegrationEventOutboxDeadLetterRow>> ListIntegrationOutboxDeadLettersAsync(
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        return _integrationEventOutbox.ListDeadLettersAsync(maxRows, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> RetryIntegrationOutboxDeadLetterAsync(Guid outboxId,
        CancellationToken cancellationToken = default)
    {
        return _integrationEventOutbox.ResetDeadLetterForRetryAsync(outboxId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DataConsistencyOrphanCounts> GetDataConsistencyOrphanCountsAsync(
        CancellationToken cancellationToken = default)
    {
        if (ArchLucidOptions.EffectiveIsInMemory(_archLucidOptions.Value.StorageProvider))
            return new DataConsistencyOrphanCounts(0, 0, 0, 0);


        DbConnection connection = (DbConnection)_connectionFactory.CreateConnection();
        await using DbConnection _ = connection;
        await connection.OpenAsync(cancellationToken);

        long left = await ExecuteCountAsync(connection, DataConsistencyOrphanProbeSql.ComparisonRecordsLeftRunId,
            cancellationToken);
        long right = await ExecuteCountAsync(connection, DataConsistencyOrphanProbeSql.ComparisonRecordsRightRunId,
            cancellationToken);
        long golden = await ExecuteCountAsync(connection, DataConsistencyOrphanProbeSql.GoldenManifestsRunId,
            cancellationToken);
        long findings = await ExecuteCountAsync(connection, DataConsistencyOrphanProbeSql.FindingsSnapshotsRunId,
            cancellationToken);

        return new DataConsistencyOrphanCounts(left, right, golden, findings);
    }

    /// <inheritdoc />
    public async Task<OrphanComparisonRemediationResult> RemediateOrphanComparisonRecordsAsync(
        bool dryRun,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        if (ArchLucidOptions.EffectiveIsInMemory(_archLucidOptions.Value.StorageProvider))
            return new OrphanComparisonRemediationResult(dryRun, 0, []);


        int capped = Math.Clamp(maxRows, 1, PaginationDefaults.MaxListingTake);
        DbConnection connection = (DbConnection)_connectionFactory.CreateConnection();
        await using DbConnection _ = connection;
        await connection.OpenAsync(cancellationToken);

        List<string> candidateIds = [];

        await using (DbCommand selectCommand = connection.CreateCommand())
        {
            selectCommand.CommandText = DataConsistencyOrphanRemediationSql.SelectOrphanComparisonRecordIds;
            DbParameter maxRowsParameter = selectCommand.CreateParameter();
            maxRowsParameter.ParameterName = "@MaxRows";
            maxRowsParameter.Value = capped;
            selectCommand.Parameters.Add(maxRowsParameter);

            await using DbDataReader reader = await selectCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))

                candidateIds.Add(reader.GetString(0));
        }

        if (dryRun)
            return new OrphanComparisonRemediationResult(true, candidateIds.Count, candidateIds);


        if (candidateIds.Count == 0)
            return new OrphanComparisonRemediationResult(false, 0, []);


        List<string> deletedIds = [];

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (DbCommand deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = DataConsistencyOrphanRemediationSql.DeleteOrphanComparisonRecordsWithOutput;
                DbParameter maxRowsParameter = deleteCommand.CreateParameter();
                maxRowsParameter.ParameterName = "@MaxRows";
                maxRowsParameter.Value = capped;
                deleteCommand.Parameters.Add(maxRowsParameter);

                await using DbDataReader reader = await deleteCommand.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))

                    deletedIds.Add(reader.GetString(0));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        if (deletedIds.Count > 0)
            await _auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.ComparisonRecordOrphansRemediated,
                    DataJson = JsonSerializer.Serialize(
                        new { dryRun = false, deletedCount = deletedIds.Count, comparisonRecordIds = deletedIds })
                },
                cancellationToken);


        return new OrphanComparisonRemediationResult(false, deletedIds.Count, deletedIds);
    }

    /// <inheritdoc />
    public async Task<OrphanGoldenManifestRemediationResult> RemediateOrphanGoldenManifestsAsync(
        bool dryRun,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        if (ArchLucidOptions.EffectiveIsInMemory(_archLucidOptions.Value.StorageProvider))
            return new OrphanGoldenManifestRemediationResult(dryRun, 0, []);


        int capped = Math.Clamp(maxRows, 1, PaginationDefaults.MaxListingTake);
        DbConnection connection = (DbConnection)_connectionFactory.CreateConnection();
        await using DbConnection _ = connection;
        await connection.OpenAsync(cancellationToken);

        List<string> candidateIds = [];

        await using (DbCommand selectCommand = connection.CreateCommand())
        {
            selectCommand.CommandText = DataConsistencyOrphanRemediationSql.SelectOrphanGoldenManifestIds;
            DbParameter maxRowsParameter = selectCommand.CreateParameter();
            maxRowsParameter.ParameterName = "@MaxRows";
            maxRowsParameter.Value = capped;
            selectCommand.Parameters.Add(maxRowsParameter);

            await using DbDataReader reader = await selectCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))

                candidateIds.Add(reader.GetGuid(0).ToString("D"));
        }

        if (dryRun)
            return new OrphanGoldenManifestRemediationResult(true, candidateIds.Count, candidateIds);


        if (candidateIds.Count == 0)
            return new OrphanGoldenManifestRemediationResult(false, 0, []);


        List<string> deletedIds = [];

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (string manifestId in candidateIds)
            {
                await using DbCommand bundleDelete = connection.CreateCommand();
                bundleDelete.Transaction = transaction;
                bundleDelete.CommandText = "DELETE FROM dbo.ArtifactBundles WHERE ManifestId = @ManifestId;";
                DbParameter mid = bundleDelete.CreateParameter();
                mid.ParameterName = "@ManifestId";
                mid.Value = Guid.Parse(manifestId, CultureInfo.InvariantCulture);
                bundleDelete.Parameters.Add(mid);
                await bundleDelete.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (string manifestId in candidateIds)
            {
                await using DbCommand deleteManifest = connection.CreateCommand();
                deleteManifest.Transaction = transaction;
                deleteManifest.CommandText = """
                                             DELETE FROM dbo.GoldenManifests
                                             OUTPUT deleted.ManifestId
                                             WHERE ManifestId = @ManifestId;
                                             """;
                DbParameter mid = deleteManifest.CreateParameter();
                mid.ParameterName = "@ManifestId";
                mid.Value = Guid.Parse(manifestId, CultureInfo.InvariantCulture);
                deleteManifest.Parameters.Add(mid);

                await using DbDataReader reader = await deleteManifest.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))

                    deletedIds.Add(reader.GetGuid(0).ToString("D"));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        if (deletedIds.Count > 0)
            await _auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.GoldenManifestOrphansRemediated,
                    DataJson = JsonSerializer.Serialize(
                        new { dryRun = false, deletedCount = deletedIds.Count, manifestIds = deletedIds })
                },
                cancellationToken);


        return new OrphanGoldenManifestRemediationResult(false, deletedIds.Count, deletedIds);
    }

    /// <inheritdoc />
    public async Task<OrphanFindingsSnapshotRemediationResult> RemediateOrphanFindingsSnapshotsAsync(
        bool dryRun,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        if (ArchLucidOptions.EffectiveIsInMemory(_archLucidOptions.Value.StorageProvider))
            return new OrphanFindingsSnapshotRemediationResult(dryRun, 0, []);


        int capped = Math.Clamp(maxRows, 1, PaginationDefaults.MaxListingTake);
        DbConnection connection = (DbConnection)_connectionFactory.CreateConnection();
        await using DbConnection _ = connection;
        await connection.OpenAsync(cancellationToken);

        List<string> candidateIds = [];

        await using (DbCommand selectCommand = connection.CreateCommand())
        {
            selectCommand.CommandText = DataConsistencyOrphanRemediationSql.SelectOrphanFindingsSnapshotIds;
            DbParameter maxRowsParameter = selectCommand.CreateParameter();
            maxRowsParameter.ParameterName = "@MaxRows";
            maxRowsParameter.Value = capped;
            selectCommand.Parameters.Add(maxRowsParameter);

            await using DbDataReader reader = await selectCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))

                candidateIds.Add(reader.GetGuid(0).ToString("D"));
        }

        if (dryRun)
            return new OrphanFindingsSnapshotRemediationResult(true, candidateIds.Count, candidateIds);


        if (candidateIds.Count == 0)
            return new OrphanFindingsSnapshotRemediationResult(false, 0, []);


        List<string> deletedIds = [];

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (string snapshotId in candidateIds)
            {
                await using DbCommand deleteSnapshot = connection.CreateCommand();
                deleteSnapshot.Transaction = transaction;
                deleteSnapshot.CommandText = """
                                             DELETE FROM dbo.FindingsSnapshots
                                             OUTPUT deleted.FindingsSnapshotId
                                             WHERE FindingsSnapshotId = @FindingsSnapshotId;
                                             """;
                DbParameter sid = deleteSnapshot.CreateParameter();
                sid.ParameterName = "@FindingsSnapshotId";
                sid.Value = Guid.Parse(snapshotId, CultureInfo.InvariantCulture);
                deleteSnapshot.Parameters.Add(sid);

                await using DbDataReader reader = await deleteSnapshot.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))

                    deletedIds.Add(reader.GetGuid(0).ToString("D"));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        if (deletedIds.Count > 0)
            await _auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.FindingsSnapshotOrphansRemediated,
                    DataJson = JsonSerializer.Serialize(
                        new { dryRun = false, deletedCount = deletedIds.Count, findingsSnapshotIds = deletedIds })
                },
                cancellationToken);


        return new OrphanFindingsSnapshotRemediationResult(false, deletedIds.Count, deletedIds);
    }

    /// <inheritdoc />
    public async Task<RunArchiveBatchResult> ArchiveRunsCreatedBeforeAsync(
        DateTimeOffset createdBeforeUtc,
        CancellationToken cancellationToken = default)
    {
        RunArchiveBatchResult result =
            await _runRepository.ArchiveRunsCreatedBeforeAsync(createdBeforeUtc, cancellationToken);

        if (result.UpdatedCount > 0)

            await LogManifestArchivedBatchAsync(
                $"createdBefore:{createdBeforeUtc.UtcDateTime:o}",
                result.ArchivedRuns.Count,
                result.ArchivedRuns.Select(static r => r.RunId.ToString("D")).ToList(),
                result.ChildCascade,
                cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<RunArchiveByIdsResult> ArchiveRunsByIdsAsync(
        IReadOnlyList<Guid> runIds,
        CancellationToken cancellationToken = default)
    {
        RunArchiveByIdsResult result = await _runRepository.ArchiveRunsByIdsAsync(runIds, cancellationToken);

        if (result.SucceededRunIds.Count > 0)

            await LogManifestArchivedBatchAsync(
                "byIds",
                result.SucceededRunIds.Count,
                result.SucceededRunIds.Select(static r => r.ToString("D")).ToList(),
                result.ChildCascade,
                cancellationToken);

        return result;
    }

    private async Task LogManifestArchivedBatchAsync(
        string kind,
        int updatedCount,
        List<string> archivedRunIdsSample,
        RunArchiveChildCascadeCounts childCascade,
        CancellationToken cancellationToken)
    {
        string actor = _actorContext.GetActor();

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.ManifestArchived,
                ActorUserId = actor,
                ActorUserName = actor,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        kind,
                        updatedRuns = updatedCount,
                        sampleRunIds = archivedRunIdsSample.Take(64).ToList(),
                        childCascade,
                    }),
            },
            cancellationToken);
    }

    private static async Task<long> ExecuteCountAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is long l ? l : Convert.ToInt64(scalar ?? 0L, CultureInfo.InvariantCulture);
    }
}
