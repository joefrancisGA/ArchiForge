using System.Data;
using System.Text.Json;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

using Dapper;

using Microsoft.Data.SqlClient;

using Dm = ArchLucid.Decisioning.Models;

namespace ArchLucid.Application.Runs.Finalization;

/// <inheritdoc cref="IManifestFinalizationService" />
public sealed class ManifestFinalizationService(
    IArchLucidUnitOfWorkFactory unitOfWorkFactory,
    IScopeContextProvider scopeContextProvider,
    IRunRepository runRepository,
    IFindingsSnapshotRepository findingsSnapshotRepository,
    IDecisionTraceRepository decisionTraceRepository,
    IGoldenManifestRepository goldenManifestRepository,
    IManifestHashService manifestHashService,
    IAuditService auditService,
    IIntegrationEventOutboxRepository integrationEventOutbox) : IManifestFinalizationService
{
    private const int SqlRunNotFoundOrScope = 50001;

    private const int SqlCommittedDifferentManifest = 50002;

    private const int SqlBadRunStatus = 50003;

    private const int SqlFindingsMismatch = 50004;

    private const int SqlArtifactMismatch = 50005;

    private const int SqlConcurrencyConflict = 50006;

    /// <inheritdoc />
    public async Task<ManifestFinalizationResult> FinalizeAsync(
        ManifestFinalizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ManifestModel);
        ArgumentNullException.ThrowIfNull(request.Contract);
        ArgumentNullException.ThrowIfNull(request.Keying);
        ArgumentNullException.ThrowIfNull(request.Trace);

        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        await using IArchLucidUnitOfWork uow = await unitOfWorkFactory.CreateAsync(cancellationToken);

        try
        {
            if (uow.SupportsExternalTransaction)
                return await FinalizeSqlAsync(scope, request, uow, cancellationToken);

            return await FinalizeLegacyAsync(scope, request, uow, cancellationToken);
        }
        catch
        {
            await uow.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<ManifestFinalizationResult> FinalizeSqlAsync(
        ScopeContext scope,
        ManifestFinalizationRequest request,
        IArchLucidUnitOfWork uow,
        CancellationToken cancellationToken)
    {
        IDbConnection connection = uow.Connection;
        IDbTransaction transaction = uow.Transaction;

        const string lockSql = """
                               SELECT LegacyRunStatus,
                                      GoldenManifestId,
                                      CurrentManifestVersion,
                                      FindingsSnapshotId,
                                      ArtifactBundleId,
                                      RowVersionStamp
                               FROM dbo.Runs WITH (UPDLOCK, ROWLOCK)
                               WHERE RunId = @RunId
                                 AND TenantId = @TenantId
                                 AND WorkspaceId = @WorkspaceId
                                 AND ScopeProjectId = @ScopeProjectId
                                 AND ArchivedUtc IS NULL;
                               """;

        LockedRunRow? locked = await connection.QuerySingleOrDefaultAsync<LockedRunRow>(
            new CommandDefinition(
                lockSql,
                new
                {
                    request.RunId,
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId
                },
                transaction,
                cancellationToken: cancellationToken));

        if (locked is null)
            throw new RunNotFoundException(request.RunId.ToString("N"));

        if (string.Equals(
                locked.LegacyRunStatus,
                nameof(ArchitectureRunStatus.Committed),
                StringComparison.OrdinalIgnoreCase))
        {
            if (locked.GoldenManifestId is not { } manifestId)
                throw new ConflictException(
                    $"Run '{request.RunId:D}' is Committed but GoldenManifestId is missing on the run record.");

            await uow.CommitAsync(cancellationToken);

            return new ManifestFinalizationResult(
                manifestId,
                WasIdempotentReturn: true,
                locked.CurrentManifestVersion ?? string.Empty,
                PersistedManifest: null);
        }

        if (!IsCommitAllowedStatus(locked.LegacyRunStatus))
            throw new ConflictException(
                $"Run '{request.RunId:D}' cannot be finalized in status '{locked.LegacyRunStatus ?? "(null)"}'.");

        if (locked.FindingsSnapshotId is null || locked.FindingsSnapshotId.Value != request.ExpectedFindingsSnapshotId)
            throw new InvalidOperationException(
                "Findings snapshot on the run record does not match the expected findings for finalization.");

        await EnsureFindingsSnapshotFinalizableAsync(request.ExpectedFindingsSnapshotId, cancellationToken);

        if (request.ExpectedArtifactBundleId is { } expectedBundle)
        {
            if (locked.ArtifactBundleId is null || locked.ArtifactBundleId.Value != expectedBundle)
                throw new InvalidOperationException(
                    "Artifact bundle on the run record does not match the expected bundle for finalization.");
        }

        RuleAuditTracePayload audit = request.Trace.RequireRuleAudit();

        await decisionTraceRepository.SaveAsync(request.Trace, cancellationToken, connection, transaction);

        Dm.ManifestDocument persisted = await goldenManifestRepository.SaveAsync(
            request.Contract,
            scope,
            request.Keying,
            manifestHashService,
            cancellationToken,
            connection,
            transaction,
            authorityPersistBody: request.ManifestModel);

        DateTime occurredUtc = DateTime.UtcNow;
        Guid auditEventId = Guid.NewGuid();
        Guid outboxId = Guid.NewGuid();

        string auditDataJson = JsonSerializer.Serialize(
            new
            {
                manifestVersion = request.Contract.Metadata.ManifestVersion,
                findingsSnapshotId = request.ExpectedFindingsSnapshotId,
                artifactBundleId = request.ExpectedArtifactBundleId,
                decisionTraceId = audit.DecisionTraceId,
                manifestId = persisted.ManifestId
            },
            IntegrationEventJson.Options);

        object outboxPayload = new
        {
            schemaVersion = 1,
            runId = request.RunId,
            manifestId = persisted.ManifestId,
            decisionTraceId = audit.DecisionTraceId,
            tenantId = scope.TenantId,
            workspaceId = scope.WorkspaceId,
            projectId = scope.ProjectId,
            findingsSnapshotId = request.ExpectedFindingsSnapshotId,
            artifactBundleId = request.ExpectedArtifactBundleId,
            manifestVersion = request.Contract.Metadata.ManifestVersion
        };

        byte[] payloadUtf8 = JsonSerializer.SerializeToUtf8Bytes(outboxPayload, IntegrationEventJson.Options);
        string messageId = $"{request.RunId:N}:{IntegrationEventTypes.ManifestFinalizedV1}";

        DynamicParameters sp = new();
        sp.Add("@TenantId", scope.TenantId);
        sp.Add("@WorkspaceId", scope.WorkspaceId);
        sp.Add("@ScopeProjectId", scope.ProjectId);
        sp.Add("@RunId", request.RunId);
        sp.Add("@ExpectedFindingsSnapshotId", request.ExpectedFindingsSnapshotId);
        sp.Add("@ExpectedArtifactBundleId", request.ExpectedArtifactBundleId);
        sp.Add("@ManifestId", persisted.ManifestId);
        sp.Add("@DecisionTraceId", audit.DecisionTraceId);
        sp.Add("@ManifestVersion", request.Contract.Metadata.ManifestVersion);
        sp.Add("@ExpectedRowVersion", locked.RowVersionStamp);
        sp.Add("@ActorUserId", request.ActorUserId);
        sp.Add("@ActorUserName", request.ActorUserName);
        sp.Add("@AuditEventId", auditEventId);
        sp.Add("@OccurredUtc", occurredUtc);
        sp.Add("@AuditDataJson", auditDataJson);
        sp.Add("@CorrelationId", request.CorrelationId);
        sp.Add("@OutboxId", outboxId);
        sp.Add("@IntegrationEventType", IntegrationEventTypes.ManifestFinalizedV1);
        sp.Add("@OutboxMessageId", messageId);
        sp.Add("@OutboxPayloadUtf8", payloadUtf8);
        sp.Add(
            "@OutboxPriority",
            IntegrationEventOutboxPriority.ForEventType(IntegrationEventTypes.ManifestFinalizedV1));

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.sp_FinalizeManifest",
                    sp,
                    transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
        }
        catch (SqlException ex)
        {
            throw MapSqlException(ex, request.RunId);
        }

        await uow.CommitAsync(cancellationToken);

        return new ManifestFinalizationResult(
            persisted.ManifestId,
            WasIdempotentReturn: false,
            request.Contract.Metadata.ManifestVersion,
            persisted);
    }

    private async Task<ManifestFinalizationResult> FinalizeLegacyAsync(
        ScopeContext scope,
        ManifestFinalizationRequest request,
        IArchLucidUnitOfWork uow,
        CancellationToken cancellationToken)
    {
        RunRecord? header = await runRepository.GetByIdAsync(scope, request.RunId, cancellationToken);

        if (header is null)
            throw new RunNotFoundException(request.RunId.ToString("N"));

        if (string.Equals(
                header.LegacyRunStatus,
                nameof(ArchitectureRunStatus.Committed),
                StringComparison.OrdinalIgnoreCase))
        {
            if (header.GoldenManifestId is not { } mid)
                throw new ConflictException(
                    $"Run '{request.RunId:D}' is Committed but GoldenManifestId is missing on the run record.");

            await uow.CommitAsync(cancellationToken);

            return new ManifestFinalizationResult(
                mid,
                WasIdempotentReturn: true,
                header.CurrentManifestVersion ?? string.Empty,
                PersistedManifest: null);
        }

        if (!IsCommitAllowedStatus(header.LegacyRunStatus))
            throw new ConflictException(
                $"Run '{request.RunId:D}' cannot be finalized in status '{header.LegacyRunStatus ?? "(null)"}'.");

        if (header.FindingsSnapshotId is null || header.FindingsSnapshotId.Value != request.ExpectedFindingsSnapshotId)
            throw new InvalidOperationException(
                "Findings snapshot on the run record does not match the expected findings for finalization.");

        await EnsureFindingsSnapshotFinalizableAsync(request.ExpectedFindingsSnapshotId, cancellationToken);

        if (request.ExpectedArtifactBundleId is { } expectedBundle)
        {
            if (header.ArtifactBundleId is null || header.ArtifactBundleId.Value != expectedBundle)
                throw new InvalidOperationException(
                    "Artifact bundle on the run record does not match the expected bundle for finalization.");
        }

        await decisionTraceRepository.SaveAsync(request.Trace, cancellationToken);

        Dm.ManifestDocument persisted = await goldenManifestRepository.SaveAsync(
            request.Contract,
            scope,
            request.Keying,
            manifestHashService,
            cancellationToken,
            authorityPersistBody: request.ManifestModel);

        RuleAuditTracePayload audit = request.Trace.RequireRuleAudit();

        header.LegacyRunStatus = nameof(ArchitectureRunStatus.Committed);
        header.CurrentManifestVersion = request.Contract.Metadata.ManifestVersion;
        header.GoldenManifestId = persisted.ManifestId;
        header.DecisionTraceId = audit.DecisionTraceId;
        header.CompletedUtc ??= DateTime.UtcNow;

        await runRepository.UpdateAsync(header, cancellationToken);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.ManifestFinalized,
                ActorUserId = request.ActorUserId,
                ActorUserName = request.ActorUserName,
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = request.RunId,
                ManifestId = persisted.ManifestId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        manifestVersion = request.Contract.Metadata.ManifestVersion,
                        findingsSnapshotId = request.ExpectedFindingsSnapshotId,
                        artifactBundleId = request.ExpectedArtifactBundleId,
                        decisionTraceId = audit.DecisionTraceId
                    },
                    IntegrationEventJson.Options),
                CorrelationId = request.CorrelationId
            },
            cancellationToken);

        object outboxPayload = new
        {
            schemaVersion = 1,
            runId = request.RunId,
            manifestId = persisted.ManifestId,
            decisionTraceId = audit.DecisionTraceId,
            tenantId = scope.TenantId,
            workspaceId = scope.WorkspaceId,
            projectId = scope.ProjectId,
            findingsSnapshotId = request.ExpectedFindingsSnapshotId,
            artifactBundleId = request.ExpectedArtifactBundleId,
            manifestVersion = request.Contract.Metadata.ManifestVersion
        };

        byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(outboxPayload, IntegrationEventJson.Options);
        string messageId = $"{request.RunId:N}:{IntegrationEventTypes.ManifestFinalizedV1}";

        await integrationEventOutbox.EnqueueAsync(
            request.RunId,
            IntegrationEventTypes.ManifestFinalizedV1,
            messageId,
            utf8,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            cancellationToken);

        await uow.CommitAsync(cancellationToken);

        return new ManifestFinalizationResult(
            persisted.ManifestId,
            WasIdempotentReturn: false,
            request.Contract.Metadata.ManifestVersion,
            persisted);
    }

    private static Exception MapSqlException(SqlException ex, Guid runId)
    {
        return ex.Number switch
        {
            SqlRunNotFoundOrScope => new RunNotFoundException(runId.ToString("N")),
            SqlBadRunStatus or SqlCommittedDifferentManifest or SqlConcurrencyConflict => new ConflictException(
                ex.Message),
            SqlFindingsMismatch or SqlArtifactMismatch => new InvalidOperationException(ex.Message, ex),
            _ => ex
        };
    }

    private static bool IsCommitAllowedStatus(string? legacyRunStatus)
    {
        return string.Equals(legacyRunStatus, nameof(ArchitectureRunStatus.ReadyForCommit), StringComparison.OrdinalIgnoreCase) || string.Equals(legacyRunStatus, nameof(ArchitectureRunStatus.TasksGenerated), StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureFindingsSnapshotFinalizableAsync(Guid findingsSnapshotId, CancellationToken cancellationToken)
    {
        Dm.FindingsSnapshot? snapshot = await findingsSnapshotRepository.GetByIdAsync(findingsSnapshotId, cancellationToken);
        if (snapshot is null)
            throw new InvalidOperationException($"Findings snapshot '{findingsSnapshotId:D}' was not found for finalization.");

        if (snapshot.GenerationStatus is FindingsSnapshotGenerationStatus.Generating or FindingsSnapshotGenerationStatus.Failed)
            throw new InvalidOperationException(
                $"Findings snapshot '{findingsSnapshotId:D}' is not eligible for finalization (GenerationStatus={snapshot.GenerationStatus}).");
    }

    private sealed class LockedRunRow
    {
        public string? LegacyRunStatus
        {
            get;
            init;
        }

        public Guid? GoldenManifestId
        {
            get;
            init;
        }

        public string? CurrentManifestVersion
        {
            get;
            init;
        }

        public Guid? FindingsSnapshotId
        {
            get;
            init;
        }

        public Guid? ArtifactBundleId
        {
            get;
            init;
        }

        public byte[] RowVersionStamp
        {
            get;
            init;
        } = null!;
    }
}
