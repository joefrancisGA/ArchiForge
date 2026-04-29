/* One active golden manifest per run; finalize SP updates run + audit + outbox (manifest/trace inserted by app in same txn). */

IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UQ_GoldenManifests_RunId_Active'
          AND object_id = OBJECT_ID(N'dbo.GoldenManifests'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_GoldenManifests_RunId_Active
        ON dbo.GoldenManifests (RunId)
        WHERE ArchivedUtc IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.sp_FinalizeManifest', N'P') IS NULL
    EXECUTE(N'CREATE PROCEDURE dbo.sp_FinalizeManifest AS BEGIN SET NOCOUNT ON; END;');
GO

ALTER PROCEDURE dbo.sp_FinalizeManifest
    @TenantId UNIQUEIDENTIFIER,
    @WorkspaceId UNIQUEIDENTIFIER,
    @ScopeProjectId UNIQUEIDENTIFIER,
    @RunId UNIQUEIDENTIFIER,
    @ExpectedFindingsSnapshotId UNIQUEIDENTIFIER,
    @ExpectedArtifactBundleId UNIQUEIDENTIFIER = NULL,
    @ManifestId UNIQUEIDENTIFIER,
    @DecisionTraceId UNIQUEIDENTIFIER,
    @ManifestVersion NVARCHAR(128),
    @ExpectedRowVersion VARBINARY(8),
    @ActorUserId NVARCHAR(200),
    @ActorUserName NVARCHAR(200),
    @AuditEventId UNIQUEIDENTIFIER,
    @OccurredUtc DATETIME2,
    @AuditDataJson NVARCHAR(MAX),
    @CorrelationId NVARCHAR(200) = NULL,
    @OutboxId UNIQUEIDENTIFIER,
    @IntegrationEventType NVARCHAR(256),
    @OutboxMessageId NVARCHAR(128),
    @OutboxPayloadUtf8 VARBINARY(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RowsUpdated INT;

    UPDATE dbo.Runs
    SET LegacyRunStatus = N'Committed',
        GoldenManifestId = @ManifestId,
        DecisionTraceId = @DecisionTraceId,
        CurrentManifestVersion = @ManifestVersion,
        CompletedUtc = COALESCE(CompletedUtc, SYSUTCDATETIME())
    WHERE RunId = @RunId
      AND TenantId = @TenantId
      AND WorkspaceId = @WorkspaceId
      AND ScopeProjectId = @ScopeProjectId
      AND LegacyRunStatus IN (N'ReadyForCommit', N'TasksGenerated')
      AND (FindingsSnapshotId IS NOT NULL AND FindingsSnapshotId = @ExpectedFindingsSnapshotId)
      AND (
            @ExpectedArtifactBundleId IS NULL
            OR (ArtifactBundleId IS NOT NULL AND ArtifactBundleId = @ExpectedArtifactBundleId)
          )
      AND RowVersionStamp = @ExpectedRowVersion
      AND ArchivedUtc IS NULL;

    SET @RowsUpdated = @@ROWCOUNT;

    IF @RowsUpdated = 1
    BEGIN
        INSERT INTO dbo.AuditEvents (
            EventId, OccurredUtc, EventType,
            ActorUserId, ActorUserName,
            TenantId, WorkspaceId, ProjectId,
            RunId, ManifestId, ArtifactId,
            DataJson, CorrelationId
        )
        VALUES (
            @AuditEventId, @OccurredUtc, N'ManifestFinalized',
            @ActorUserId, @ActorUserName,
            @TenantId, @WorkspaceId, @ScopeProjectId,
            @RunId, @ManifestId, NULL,
            @AuditDataJson, @CorrelationId
        );

        INSERT INTO dbo.IntegrationEventOutbox (
            OutboxId, RunId, EventType, MessageId, PayloadUtf8,
            TenantId, WorkspaceId, ProjectId, CreatedUtc
        )
        VALUES (
            @OutboxId, @RunId, @IntegrationEventType, @OutboxMessageId, @OutboxPayloadUtf8,
            @TenantId, @WorkspaceId, @ScopeProjectId, SYSUTCDATETIME()
        );

        RETURN;
    END;

    DECLARE @Status NVARCHAR(64);
    DECLARE @ExistingManifest UNIQUEIDENTIFIER;
    DECLARE @RunFindings UNIQUEIDENTIFIER;
    DECLARE @RunArtifact UNIQUEIDENTIFIER;

    SELECT
        @Status = LegacyRunStatus,
        @ExistingManifest = GoldenManifestId,
        @RunFindings = FindingsSnapshotId,
        @RunArtifact = ArtifactBundleId
    FROM dbo.Runs
    WHERE RunId = @RunId
      AND TenantId = @TenantId
      AND WorkspaceId = @WorkspaceId
      AND ScopeProjectId = @ScopeProjectId
      AND ArchivedUtc IS NULL;

    IF @@ROWCOUNT = 0
        THROW 50001, N'Run not found or scope mismatch.', 1;

    IF @Status = N'Committed' AND @ExistingManifest IS NOT NULL AND @ExistingManifest = @ManifestId
        RETURN;

    IF @Status = N'Committed'
        THROW 50002, N'Run is already committed with a different golden manifest.', 1;

    IF @RunFindings IS NULL OR @RunFindings <> @ExpectedFindingsSnapshotId
        THROW 50004, N'FindingsSnapshotId does not match the run record.', 1;

    IF @ExpectedArtifactBundleId IS NOT NULL
       AND (@RunArtifact IS NULL OR @RunArtifact <> @ExpectedArtifactBundleId)
        THROW 50005, N'ArtifactBundleId does not match the run record.', 1;

    IF @Status NOT IN (N'ReadyForCommit', N'TasksGenerated')
        THROW 50003, N'Run cannot be finalized in this status.', 1;

    THROW 50006, N'Concurrency conflict or stale run row version.', 1;
END;
GO
