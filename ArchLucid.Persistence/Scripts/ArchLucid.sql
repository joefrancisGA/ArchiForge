/*
  ArchiForge — SQL Server consolidated schema (idempotent)

  DOCUMENTATION
    Full guide: docs/SQL_SCRIPTS.md (DbUp vs this file; migration catalog;
    run header model; change checklist; troubleshooting).

  EXECUTION
    - Persistence: SqlSchemaBootstrapper reads ArchiForge.Persistence/Scripts/ArchiForge.sql
      (MSBuild-linked copy of this file), splits on GO, executes each batch via Dapper.
    - Manual / SSMS: run as-is; requires SQL Server 2014+ style inline INDEX on CREATE TABLE.

  SEMANTICS
    - Safe to run multiple times: CREATE TABLE only if missing (IF OBJECT_ID … IS NULL).
    - Nonclustered indexes: inline INDEX … NONCLUSTERED inside CREATE TABLE (incl. filtered).
    - Column additions for existing DBs: use DbUp migrations in ArchiForge.Persistence/Migrations/;
      this script assumes greenfield CREATE or migrations already applied.
    - FK repair: some batches ALTER TABLE ADD CONSTRAINT FK … IF NOT EXISTS (sys.foreign_keys).

  CONTENT OVERVIEW
    - Core / Agents / Manifest & evidence / RunExportRecords / ComparisonRecords /
      DecisionNodes & AgentEvaluations (≈ DbUp 001–007 + labels/tags; run header is dbo.Runs after 049).
    - Authority + Dapper + Decisioning: dbo.Runs (UNIQUEIDENTIFIER), snapshots, manifests,
      bundles, audit, provenance, conversations, recommendations, advisory, digests, alerts,
      composite rules, policy packs (aligned with Persistence repositories).

  NOTE: Coordinator NVARCHAR RunId columns align with authority dbo.Runs.RunId as 32-char hex (N format);
    dbo.Runs is the sole persisted run header (migration 049 dropped legacy dbo.ArchitectureRuns).

  SET ANSI_NULLS ON;
  SET QUOTED_IDENTIFIER ON;
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---- Core ---- */

IF OBJECT_ID(N'dbo.ArchitectureRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArchitectureRequests
    (
        RequestId            NVARCHAR(64)  NOT NULL PRIMARY KEY,
        SystemName           NVARCHAR(200) NOT NULL,
        Environment          NVARCHAR(50)  NOT NULL,
        CloudProvider        NVARCHAR(50)  NOT NULL,
        RequestJson          NVARCHAR(MAX) NOT NULL,
        CreatedUtc           DATETIME2     NOT NULL
    );
END
GO

/* ---- Agents ---- */

IF OBJECT_ID(N'dbo.AgentTasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentTasks
    (
        TaskId             NVARCHAR(64)  NOT NULL PRIMARY KEY,
        RunId              NVARCHAR(64)  NOT NULL,
        AgentType          NVARCHAR(50)  NOT NULL,
        Objective          NVARCHAR(MAX) NOT NULL,
        Status             NVARCHAR(50)  NOT NULL,
        CreatedUtc         DATETIME2     NOT NULL,
        CompletedUtc       DATETIME2     NULL,
        EvidenceBundleRef  NVARCHAR(64)  NULL,
        INDEX IX_AgentTasks_RunId NONCLUSTERED (RunId),
        INDEX IX_AgentTasks_RunId_AgentType NONCLUSTERED (RunId, AgentType)
    );
END
GO

IF OBJECT_ID(N'dbo.AgentResults', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentResults
    (
        ResultId   NVARCHAR(64)  NOT NULL PRIMARY KEY,
        TaskId     NVARCHAR(64)  NOT NULL,
        RunId      NVARCHAR(64)  NOT NULL,
        AgentType  NVARCHAR(50)  NOT NULL,
        Confidence FLOAT         NOT NULL,
        ResultJson NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2     NOT NULL,
        CONSTRAINT FK_AgentResults_Task FOREIGN KEY (TaskId) REFERENCES dbo.AgentTasks (TaskId),
        INDEX IX_AgentResults_RunId NONCLUSTERED (RunId),
        INDEX IX_AgentResults_TaskId NONCLUSTERED (TaskId),
        INDEX IX_AgentResults_CreatedUtc NONCLUSTERED (CreatedUtc DESC)
    );
END
GO

IF OBJECT_ID(N'dbo.AgentResults', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentResults_Task')
        ALTER TABLE dbo.AgentResults ADD CONSTRAINT FK_AgentResults_Task FOREIGN KEY (TaskId)
            REFERENCES dbo.AgentTasks (TaskId);
END
GO

/* ---- Manifest / evidence ---- */

IF OBJECT_ID(N'dbo.GoldenManifestVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestVersions
    (
        ManifestVersion        NVARCHAR(50)  NOT NULL PRIMARY KEY,
        RunId                  NVARCHAR(64)  NOT NULL,
        SystemName             NVARCHAR(200) NOT NULL,
        ManifestJson           NVARCHAR(MAX) NOT NULL,
        ParentManifestVersion  NVARCHAR(50)  NULL,
        CreatedUtc             DATETIME2     NOT NULL,
        CONSTRAINT FK_GoldenManifestVersions_Parent FOREIGN KEY (ParentManifestVersion)
            REFERENCES dbo.GoldenManifestVersions (ManifestVersion),
        INDEX IX_GoldenManifestVersions_RunId NONCLUSTERED (RunId)
    );
END
GO

IF OBJECT_ID(N'dbo.GoldenManifestVersions', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifestVersions_Parent')
        ALTER TABLE dbo.GoldenManifestVersions ADD CONSTRAINT FK_GoldenManifestVersions_Parent
            FOREIGN KEY (ParentManifestVersion) REFERENCES dbo.GoldenManifestVersions (ManifestVersion);
END
GO

IF OBJECT_ID(N'dbo.EvidenceBundles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EvidenceBundles
    (
        EvidenceBundleId   NVARCHAR(64)  NOT NULL PRIMARY KEY,
        RequestDescription NVARCHAR(MAX) NOT NULL,
        EvidenceJson       NVARCHAR(MAX) NOT NULL,
        CreatedUtc         DATETIME2     NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.DecisionTraces', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DecisionTraces
    (
        TraceId          NVARCHAR(64)  NOT NULL PRIMARY KEY,
        RunId            NVARCHAR(64)  NOT NULL,
        EventType        NVARCHAR(100) NOT NULL,
        EventDescription NVARCHAR(MAX) NOT NULL,
        EventJson        NVARCHAR(MAX) NOT NULL,
        CreatedUtc       DATETIME2     NOT NULL,
        INDEX IX_DecisionTraces_RunId NONCLUSTERED (RunId),
        INDEX IX_DecisionTraces_CreatedUtc NONCLUSTERED (CreatedUtc DESC)
    );
END
GO

IF OBJECT_ID(N'dbo.AgentEvidencePackages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentEvidencePackages
    (
        EvidencePackageId NVARCHAR(64)  NOT NULL PRIMARY KEY,
        RunId             NVARCHAR(64)  NOT NULL,
        RequestId         NVARCHAR(64)  NOT NULL,
        SystemName        NVARCHAR(200) NOT NULL,
        Environment       NVARCHAR(50)  NOT NULL,
        CloudProvider     NVARCHAR(50)  NOT NULL,
        EvidenceJson      NVARCHAR(MAX) NOT NULL,
        CreatedUtc        DATETIME2     NOT NULL,
        CONSTRAINT FK_AgentEvidencePackages_Request FOREIGN KEY (RequestId)
            REFERENCES dbo.ArchitectureRequests (RequestId),
        INDEX IX_AgentEvidencePackages_RunId NONCLUSTERED (RunId)
    );
END
GO

IF OBJECT_ID(N'dbo.AgentEvidencePackages', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvidencePackages_Request')
        ALTER TABLE dbo.AgentEvidencePackages ADD CONSTRAINT FK_AgentEvidencePackages_Request
            FOREIGN KEY (RequestId) REFERENCES dbo.ArchitectureRequests (RequestId);
END
GO

IF OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentExecutionTraces
    (
        TraceId        NVARCHAR(64)  NOT NULL PRIMARY KEY,
        RunId          NVARCHAR(64)  NOT NULL,
        TaskId         NVARCHAR(64)  NOT NULL,
        AgentType      NVARCHAR(50)  NOT NULL,
        ParseSucceeded BIT           NOT NULL,
        ErrorMessage   NVARCHAR(MAX) NULL,
        TraceJson      NVARCHAR(MAX) NOT NULL,
        CreatedUtc     DATETIME2     NOT NULL,
        CONSTRAINT FK_AgentExecutionTraces_Task FOREIGN KEY (TaskId)
            REFERENCES dbo.AgentTasks (TaskId),
        INDEX IX_AgentExecutionTraces_RunId NONCLUSTERED (RunId),
        INDEX IX_AgentExecutionTraces_TaskId NONCLUSTERED (TaskId)
    );
END
GO

IF OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentExecutionTraces_Task')
        ALTER TABLE dbo.AgentExecutionTraces ADD CONSTRAINT FK_AgentExecutionTraces_Task FOREIGN KEY (TaskId)
            REFERENCES dbo.AgentTasks (TaskId);
END
GO

IF OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullSystemPromptBlobKey') IS NULL
        ALTER TABLE dbo.AgentExecutionTraces ADD FullSystemPromptBlobKey NVARCHAR(2048) NULL;

    IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullUserPromptBlobKey') IS NULL
        ALTER TABLE dbo.AgentExecutionTraces ADD FullUserPromptBlobKey NVARCHAR(2048) NULL;

    IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullResponseBlobKey') IS NULL
        ALTER TABLE dbo.AgentExecutionTraces ADD FullResponseBlobKey NVARCHAR(2048) NULL;

    IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'ModelDeploymentName') IS NULL
        ALTER TABLE dbo.AgentExecutionTraces ADD ModelDeploymentName NVARCHAR(260) NULL;

    IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'ModelVersion') IS NULL
        ALTER TABLE dbo.AgentExecutionTraces ADD ModelVersion NVARCHAR(200) NULL;

    IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'BlobUploadFailed') IS NULL
        ALTER TABLE dbo.AgentExecutionTraces ADD BlobUploadFailed BIT NULL;

    IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullSystemPromptInline') IS NULL
        ALTER TABLE dbo.AgentExecutionTraces ADD FullSystemPromptInline NVARCHAR(MAX) NULL;

    IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullUserPromptInline') IS NULL
        ALTER TABLE dbo.AgentExecutionTraces ADD FullUserPromptInline NVARCHAR(MAX) NULL;

    IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullResponseInline') IS NULL
        ALTER TABLE dbo.AgentExecutionTraces ADD FullResponseInline NVARCHAR(MAX) NULL;

    IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'InlineFallbackFailed') IS NULL
        ALTER TABLE dbo.AgentExecutionTraces ADD InlineFallbackFailed BIT NULL;
END
GO

/* Brownfield: soft-archive on dbo.AgentExecutionTraces (DbUp 073 parity; cascaded when dbo.Runs bulk-archive). */
IF OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AgentExecutionTraces', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.AgentExecutionTraces ADD ArchivedUtc DATETIME2 NULL;
GO

/* ---- AgentOutputEvaluationResults (reference-case scores) ---- */

IF OBJECT_ID(N'dbo.AgentOutputEvaluationResults', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentOutputEvaluationResults
    (
        EvaluationId     UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_AgentOutputEvaluationResults_EvaluationId DEFAULT (NEWSEQUENTIALID()),
        RunId            NVARCHAR(64)     NOT NULL,
        TraceId          NVARCHAR(64)     NOT NULL,
        CaseId           NVARCHAR(128)    NOT NULL,
        AgentType        NVARCHAR(50)     NOT NULL,
        OverallScore     FLOAT            NOT NULL,
        StructuralMatch  FLOAT            NULL,
        SemanticMatch    FLOAT            NULL,
        MissingKeysJson  NVARCHAR(MAX)    NULL,
        CreatedUtc       DATETIME2        NOT NULL,
        CONSTRAINT PK_AgentOutputEvaluationResults PRIMARY KEY (EvaluationId)
    );

    CREATE NONCLUSTERED INDEX IX_AgentOutputEvaluationResults_RunId_CreatedUtc
        ON dbo.AgentOutputEvaluationResults (RunId, CreatedUtc DESC);
END
GO

/* ---- RunExportRecords: create or extend ---- */

IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RunExportRecords
    (
        ExportRecordId               NVARCHAR(64)  NOT NULL PRIMARY KEY,
        RunId                        NVARCHAR(64)  NOT NULL,
        ExportType                   NVARCHAR(100) NOT NULL,
        Format                       NVARCHAR(50)  NOT NULL,
        FileName                     NVARCHAR(260) NOT NULL,
        TemplateProfile              NVARCHAR(100) NULL,
        TemplateProfileDisplayName   NVARCHAR(200) NULL,
        WasAutoSelected              BIT           NOT NULL,
        ResolutionReason             NVARCHAR(MAX) NULL,
        ManifestVersion              NVARCHAR(100) NULL,
        Notes                        NVARCHAR(MAX) NULL,
        AnalysisRequestJson          NVARCHAR(MAX) NULL,
        IncludedEvidence             BIT           NULL,
        IncludedExecutionTraces      BIT           NULL,
        IncludedManifest             BIT           NULL,
        IncludedDiagram              BIT           NULL,
        IncludedSummary              BIT           NULL,
        IncludedDeterminismCheck     BIT           NULL,
        DeterminismIterations        INT           NULL,
        IncludedManifestCompare      BIT           NULL,
        CompareManifestVersion       NVARCHAR(100) NULL,
        IncludedAgentResultCompare   BIT           NULL,
        CompareRunId                 NVARCHAR(64)  NULL,
        RecordJson                   NVARCHAR(MAX) NOT NULL,
        CreatedUtc                   DATETIME2     NOT NULL,
        INDEX IX_RunExportRecords_RunId NONCLUSTERED (RunId),
        INDEX IX_RunExportRecords_CreatedUtc NONCLUSTERED (CreatedUtc DESC)
    );
END
GO

/* RunExportRecords: full column set is in CREATE above (matches DbUp 001); no per-column ALTERs. */

/* ---- ComparisonRecords ---- */

IF OBJECT_ID(N'dbo.ComparisonRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComparisonRecords
    (
        ComparisonRecordId    NVARCHAR(64)  NOT NULL PRIMARY KEY,
        ComparisonType        NVARCHAR(100) NOT NULL,
        LeftRunId             NVARCHAR(64)  NULL,
        RightRunId            NVARCHAR(64)  NULL,
        LeftManifestVersion   NVARCHAR(100) NULL,
        RightManifestVersion  NVARCHAR(100) NULL,
        LeftExportRecordId    NVARCHAR(64)  NULL,
        RightExportRecordId   NVARCHAR(64)  NULL,
        Format                NVARCHAR(50)  NOT NULL,
        SummaryMarkdown       NVARCHAR(MAX) NULL,
        PayloadJson           NVARCHAR(MAX) NOT NULL,
        Notes                 NVARCHAR(MAX) NULL,
        CreatedUtc            DATETIME2     NOT NULL,
        Label                 NVARCHAR(256) NULL,
        Tags                  NVARCHAR(MAX) NULL,
        INDEX IX_ComparisonRecords_LeftRunId NONCLUSTERED (LeftRunId),
        INDEX IX_ComparisonRecords_RightRunId NONCLUSTERED (RightRunId),
        INDEX IX_ComparisonRecords_LeftExportRecordId NONCLUSTERED (LeftExportRecordId),
        INDEX IX_ComparisonRecords_RightExportRecordId NONCLUSTERED (RightExportRecordId),
        INDEX IX_ComparisonRecords_ComparisonType_CreatedUtc NONCLUSTERED (ComparisonType, CreatedUtc DESC),
        INDEX IX_ComparisonRecords_Label NONCLUSTERED (Label) WHERE (Label IS NOT NULL)
    );
END
GO

/* Brownfield: soft-archive on dbo.ComparisonRecords (DbUp 073 parity; cascaded when dbo.Runs bulk-archive). */
IF OBJECT_ID(N'dbo.ComparisonRecords', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ComparisonRecords', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.ComparisonRecords ADD ArchivedUtc DATETIME2 NULL;
GO

/* ---- Decision Engine v2 ---- */

IF OBJECT_ID(N'dbo.DecisionNodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DecisionNodes
    (
        DecisionId       NVARCHAR(64)  NOT NULL PRIMARY KEY,
        RunId            NVARCHAR(64)  NOT NULL,
        Topic            NVARCHAR(100) NOT NULL,
        SelectedOptionId NVARCHAR(64)  NULL,
        Confidence       FLOAT         NOT NULL,
        Rationale        NVARCHAR(MAX) NOT NULL,
        DecisionJson     NVARCHAR(MAX) NOT NULL,
        CreatedUtc       DATETIME2     NOT NULL,
        INDEX IX_DecisionNodes_RunId NONCLUSTERED (RunId),
        INDEX IX_DecisionNodes_CreatedUtc NONCLUSTERED (CreatedUtc DESC)
    );
END
GO

IF OBJECT_ID(N'dbo.AgentEvaluations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgentEvaluations
    (
        EvaluationId       NVARCHAR(64)  NOT NULL PRIMARY KEY,
        RunId              NVARCHAR(64)  NOT NULL,
        TargetAgentTaskId  NVARCHAR(64)  NOT NULL,
        EvaluationType     NVARCHAR(50)  NOT NULL,
        ConfidenceDelta    FLOAT         NOT NULL,
        Rationale          NVARCHAR(MAX) NOT NULL,
        EvaluationJson     NVARCHAR(MAX) NOT NULL,
        CreatedUtc         DATETIME2     NOT NULL,
        CONSTRAINT FK_AgentEvaluations_Task FOREIGN KEY (TargetAgentTaskId)
            REFERENCES dbo.AgentTasks (TaskId),
        INDEX IX_AgentEvaluations_RunId NONCLUSTERED (RunId),
        INDEX IX_AgentEvaluations_TargetAgentTaskId NONCLUSTERED (TargetAgentTaskId)
    );
END
GO

IF OBJECT_ID(N'dbo.AgentEvaluations', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvaluations_Task')
        ALTER TABLE dbo.AgentEvaluations ADD CONSTRAINT FK_AgentEvaluations_Task FOREIGN KEY (TargetAgentTaskId)
            REFERENCES dbo.AgentTasks (TaskId);
END
GO

/* ---- Authority / Dapper persistence + Decisioning (GUID dbo.Runs) ---- */
/*
  DecisioningTraces is used instead of DecisionTraces because dbo.DecisionTraces already
  exists for the API/commit trail above.
*/

IF OBJECT_ID('dbo.Runs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Runs
    (
        RunId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ProjectId NVARCHAR(200) NOT NULL,
        Description NVARCHAR(4000) NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ContextSnapshotId UNIQUEIDENTIFIER NULL,
        GraphSnapshotId UNIQUEIDENTIFIER NULL,
        FindingsSnapshotId UNIQUEIDENTIFIER NULL,
        GoldenManifestId UNIQUEIDENTIFIER NULL,
        DecisionTraceId UNIQUEIDENTIFIER NULL,
        ArtifactBundleId UNIQUEIDENTIFIER NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ScopeProjectId UNIQUEIDENTIFIER NOT NULL,
        ArchivedUtc DATETIME2 NULL,
        ArchitectureRequestId NVARCHAR(64) NULL,
        LegacyRunStatus NVARCHAR(64) NULL,
        CompletedUtc DATETIME2 NULL,
        CurrentManifestVersion NVARCHAR(128) NULL,
        OtelTraceId NVARCHAR(64) NULL,
        RowVersionStamp ROWVERSION,
        INDEX IX_Runs_ProjectId_CreatedUtc NONCLUSTERED (ProjectId, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'RowVersionStamp') IS NULL
    ALTER TABLE dbo.Runs ADD RowVersionStamp ROWVERSION;
GO

IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'OtelTraceId') IS NULL
    ALTER TABLE dbo.Runs ADD OtelTraceId NVARCHAR(64) NULL;
GO

IF OBJECT_ID('dbo.ContextSnapshots', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshots
    (
        SnapshotId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ProjectId NVARCHAR(200) NOT NULL,
        TenantId UNIQUEIDENTIFIER NULL,
        WorkspaceId UNIQUEIDENTIFIER NULL,
        ScopeProjectId UNIQUEIDENTIFIER NULL,
        CreatedUtc DATETIME2 NOT NULL,
        CanonicalObjectsJson NVARCHAR(MAX) NOT NULL,
        DeltaSummary NVARCHAR(MAX) NULL,
        WarningsJson NVARCHAR(MAX) NOT NULL,
        ErrorsJson NVARCHAR(MAX) NOT NULL,
        SourceHashesJson NVARCHAR(MAX) NOT NULL,
        INDEX IX_ContextSnapshots_ProjectId_CreatedUtc NONCLUSTERED (ProjectId, CreatedUtc DESC),
        INDEX IX_ContextSnapshots_RunId NONCLUSTERED (RunId)
    );
END;
GO

/* Brownfield: RLS scope denormalization (DbUp 046 parity) on dbo.ContextSnapshots */
IF OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ContextSnapshots', N'TenantId') IS NULL
        ALTER TABLE dbo.ContextSnapshots ADD TenantId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.ContextSnapshots', N'WorkspaceId') IS NULL
        ALTER TABLE dbo.ContextSnapshots ADD WorkspaceId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.ContextSnapshots', N'ScopeProjectId') IS NULL
        ALTER TABLE dbo.ContextSnapshots ADD ScopeProjectId UNIQUEIDENTIFIER NULL;
END;
GO

/* Brownfield: soft-archive on dbo.ContextSnapshots (DbUp 067 parity; cascaded when dbo.Runs bulk-archive). */
IF OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ContextSnapshots', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.ContextSnapshots ADD ArchivedUtc DATETIME2 NULL;
GO

/* Relational expansion for dbo.ContextSnapshots (dual-write; legacy JSON columns retained). */
IF OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshotCanonicalObjects
    (
        CanonicalObjectRowId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_ContextSnapshotCanonicalObjects PRIMARY KEY,
        SnapshotId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        ObjectId NVARCHAR(450) NOT NULL,
        ObjectType NVARCHAR(200) NOT NULL,
        Name NVARCHAR(500) NOT NULL,
        SourceType NVARCHAR(200) NOT NULL,
        SourceId NVARCHAR(450) NOT NULL,
        CONSTRAINT FK_ContextSnapshotCanonicalObjects_ContextSnapshots FOREIGN KEY (SnapshotId)
            REFERENCES dbo.ContextSnapshots (SnapshotId) ON DELETE CASCADE,
        CONSTRAINT UQ_ContextSnapshotCanonicalObjects_Snapshot_Sort UNIQUE (SnapshotId, SortOrder)
    );

    CREATE NONCLUSTERED INDEX IX_ContextSnapshotCanonicalObjects_SnapshotId
        ON dbo.ContextSnapshotCanonicalObjects (SnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotCanonicalObjectProperties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshotCanonicalObjectProperties
    (
        CanonicalObjectRowId UNIQUEIDENTIFIER NOT NULL,
        PropertySortOrder INT NOT NULL,
        PropertyKey NVARCHAR(200) NOT NULL,
        PropertyValue NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ContextSnapshotCanonicalObjectProperties PRIMARY KEY (CanonicalObjectRowId, PropertySortOrder),
        CONSTRAINT FK_ContextSnapshotCanonicalObjectProperties_Objects FOREIGN KEY (CanonicalObjectRowId)
            REFERENCES dbo.ContextSnapshotCanonicalObjects (CanonicalObjectRowId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ContextSnapshotCanonicalObjectProperties_Object
        ON dbo.ContextSnapshotCanonicalObjectProperties (CanonicalObjectRowId);
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotWarnings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshotWarnings
    (
        SnapshotId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        WarningText NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ContextSnapshotWarnings PRIMARY KEY (SnapshotId, SortOrder),
        CONSTRAINT FK_ContextSnapshotWarnings_ContextSnapshots FOREIGN KEY (SnapshotId)
            REFERENCES dbo.ContextSnapshots (SnapshotId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ContextSnapshotWarnings_SnapshotId
        ON dbo.ContextSnapshotWarnings (SnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotErrors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshotErrors
    (
        SnapshotId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        ErrorText NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ContextSnapshotErrors PRIMARY KEY (SnapshotId, SortOrder),
        CONSTRAINT FK_ContextSnapshotErrors_ContextSnapshots FOREIGN KEY (SnapshotId)
            REFERENCES dbo.ContextSnapshots (SnapshotId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ContextSnapshotErrors_SnapshotId
        ON dbo.ContextSnapshotErrors (SnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshotSourceHashes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshotSourceHashes
    (
        SnapshotId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        SourceKey NVARCHAR(450) NOT NULL,
        HashValue NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ContextSnapshotSourceHashes PRIMARY KEY (SnapshotId, SortOrder),
        CONSTRAINT FK_ContextSnapshotSourceHashes_ContextSnapshots FOREIGN KEY (SnapshotId)
            REFERENCES dbo.ContextSnapshots (SnapshotId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ContextSnapshotSourceHashes_SnapshotId
        ON dbo.ContextSnapshotSourceHashes (SnapshotId);
END;
GO

IF OBJECT_ID('dbo.GraphSnapshots', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GraphSnapshots
    (
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ContextSnapshotId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        NodesJson NVARCHAR(MAX) NULL,
        EdgesJson NVARCHAR(MAX) NULL,
        WarningsJson NVARCHAR(MAX) NULL,
        INDEX IX_GraphSnapshots_RunId NONCLUSTERED (RunId),
        INDEX IX_GraphSnapshots_ContextSnapshotId NONCLUSTERED (ContextSnapshotId)
    );
END;
GO

/* GraphSnapshots legacy JSON columns nullable (see Migrations/042_GraphSnapshots_LegacyJsonNullable.sql). */
IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON c.object_id = t.object_id
        WHERE t.schema_id = SCHEMA_ID(N'dbo')
          AND t.name = N'GraphSnapshots'
          AND c.name = N'NodesJson'
          AND c.is_nullable = 0)
        ALTER TABLE dbo.GraphSnapshots ALTER COLUMN NodesJson NVARCHAR(MAX) NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON c.object_id = t.object_id
        WHERE t.schema_id = SCHEMA_ID(N'dbo')
          AND t.name = N'GraphSnapshots'
          AND c.name = N'EdgesJson'
          AND c.is_nullable = 0)
        ALTER TABLE dbo.GraphSnapshots ALTER COLUMN EdgesJson NVARCHAR(MAX) NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON c.object_id = t.object_id
        WHERE t.schema_id = SCHEMA_ID(N'dbo')
          AND t.name = N'GraphSnapshots'
          AND c.name = N'WarningsJson'
          AND c.is_nullable = 0)
        ALTER TABLE dbo.GraphSnapshots ALTER COLUMN WarningsJson NVARCHAR(MAX) NULL;
END;
GO

/* Brownfield: soft-archive on dbo.GraphSnapshots (DbUp 067 parity; cascaded when dbo.Runs bulk-archive). */
IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GraphSnapshots', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.GraphSnapshots ADD ArchivedUtc DATETIME2 NULL;
GO

IF OBJECT_ID('dbo.GraphSnapshotEdges', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GraphSnapshotEdges
    (
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL,
        EdgeId            NVARCHAR(200) NOT NULL,
        FromNodeId        NVARCHAR(500) NOT NULL,
        ToNodeId          NVARCHAR(500) NOT NULL,
        EdgeType          NVARCHAR(100) NOT NULL,
        Weight            FLOAT NOT NULL
            CONSTRAINT DF_GraphSnapshotEdges_Weight DEFAULT (1),
        CONSTRAINT PK_GraphSnapshotEdges PRIMARY KEY (GraphSnapshotId, EdgeId),
        CONSTRAINT FK_GraphSnapshotEdges_GraphSnapshots FOREIGN KEY (GraphSnapshotId)
            REFERENCES dbo.GraphSnapshots (GraphSnapshotId)
    );

    -- Key length must stay within SQL Server 1700-byte nonclustered index limit (avoid three wide NVARCHARs in the key).
    CREATE NONCLUSTERED INDEX IX_GraphSnapshotEdges_SnapshotFrom
        ON dbo.GraphSnapshotEdges (GraphSnapshotId, FromNodeId)
        INCLUDE (ToNodeId, EdgeType, Weight);
END;
GO

-- Relational children for GraphSnapshots (dual-write; JSON columns retained). GraphSnapshotEdges remains authoritative for indexed edge queries.
IF OBJECT_ID(N'dbo.GraphSnapshotNodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GraphSnapshotNodes
    (
        GraphNodeRowId   UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_GraphSnapshotNodes PRIMARY KEY,
        GraphSnapshotId  UNIQUEIDENTIFIER NOT NULL,
        SortOrder        INT NOT NULL,
        NodeId           NVARCHAR(500) NOT NULL,
        NodeType         NVARCHAR(100) NOT NULL,
        Label            NVARCHAR(1000) NOT NULL,
        Category         NVARCHAR(200) NULL,
        SourceType       NVARCHAR(200) NULL,
        SourceId         NVARCHAR(500) NULL,
        CONSTRAINT FK_GraphSnapshotNodes_GraphSnapshots FOREIGN KEY (GraphSnapshotId)
            REFERENCES dbo.GraphSnapshots (GraphSnapshotId) ON DELETE CASCADE,
        CONSTRAINT UQ_GraphSnapshotNodes_Snapshot_Sort UNIQUE (GraphSnapshotId, SortOrder)
    );

    CREATE NONCLUSTERED INDEX IX_GraphSnapshotNodes_SnapshotId
        ON dbo.GraphSnapshotNodes (GraphSnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotNodeProperties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GraphSnapshotNodeProperties
    (
        GraphNodeRowId    UNIQUEIDENTIFIER NOT NULL,
        PropertySortOrder INT NOT NULL,
        PropertyKey       NVARCHAR(200) NOT NULL,
        PropertyValue     NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_GraphSnapshotNodeProperties PRIMARY KEY (GraphNodeRowId, PropertySortOrder),
        CONSTRAINT FK_GraphSnapshotNodeProperties_Nodes FOREIGN KEY (GraphNodeRowId)
            REFERENCES dbo.GraphSnapshotNodes (GraphNodeRowId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GraphSnapshotNodeProperties_Node
        ON dbo.GraphSnapshotNodeProperties (GraphNodeRowId);
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotEdgeProperties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GraphSnapshotEdgeProperties
    (
        GraphSnapshotId   UNIQUEIDENTIFIER NOT NULL,
        EdgeId            NVARCHAR(200) NOT NULL,
        PropertySortOrder INT NOT NULL,
        PropertyKey       NVARCHAR(200) NOT NULL,
        PropertyValue     NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_GraphSnapshotEdgeProperties PRIMARY KEY (GraphSnapshotId, EdgeId, PropertySortOrder),
        CONSTRAINT FK_GraphSnapshotEdgeProperties_Edges FOREIGN KEY (GraphSnapshotId, EdgeId)
            REFERENCES dbo.GraphSnapshotEdges (GraphSnapshotId, EdgeId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GraphSnapshotEdgeProperties_SnapshotId
        ON dbo.GraphSnapshotEdgeProperties (GraphSnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshotWarnings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GraphSnapshotWarnings
    (
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        WarningText     NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_GraphSnapshotWarnings PRIMARY KEY (GraphSnapshotId, SortOrder),
        CONSTRAINT FK_GraphSnapshotWarnings_GraphSnapshots FOREIGN KEY (GraphSnapshotId)
            REFERENCES dbo.GraphSnapshots (GraphSnapshotId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GraphSnapshotWarnings_SnapshotId
        ON dbo.GraphSnapshotWarnings (GraphSnapshotId);
END;
GO

IF OBJECT_ID('dbo.FindingsSnapshots', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingsSnapshots
    (
        FindingsSnapshotId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ContextSnapshotId UNIQUEIDENTIFIER NOT NULL,
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NULL,
        WorkspaceId UNIQUEIDENTIFIER NULL,
        ProjectId UNIQUEIDENTIFIER NULL,
        CreatedUtc DATETIME2 NOT NULL,
        SchemaVersion INT NOT NULL DEFAULT (1),
        FindingsJson NVARCHAR(MAX) NOT NULL,
        INDEX IX_FindingsSnapshots_RunId NONCLUSTERED (RunId),
        INDEX IX_FindingsSnapshots_ContextSnapshotId NONCLUSTERED (ContextSnapshotId),
        INDEX IX_FindingsSnapshots_GraphSnapshotId NONCLUSTERED (GraphSnapshotId)
    );
END;
GO

/* Brownfield: RLS scope denormalization (DbUp 046 parity) on dbo.FindingsSnapshots */
IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FindingsSnapshots', N'TenantId') IS NULL
        ALTER TABLE dbo.FindingsSnapshots ADD TenantId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.FindingsSnapshots', N'WorkspaceId') IS NULL
        ALTER TABLE dbo.FindingsSnapshots ADD WorkspaceId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.FindingsSnapshots', N'ProjectId') IS NULL
        ALTER TABLE dbo.FindingsSnapshots ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

-- Brownfield: SchemaVersion on FindingsSnapshots (relational reads use header; JSON fallback still migrates in app).
IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.FindingsSnapshots', N'SchemaVersion') IS NULL
BEGIN
    ALTER TABLE dbo.FindingsSnapshots
        ADD SchemaVersion INT NOT NULL CONSTRAINT DF_FindingsSnapshots_SchemaVersion_Brownfield DEFAULT (1);
END;
GO

/* Brownfield: soft-archive on dbo.FindingsSnapshots (DbUp 066 parity; cascaded when dbo.Runs bulk-archive). */
IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.FindingsSnapshots', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.FindingsSnapshots ADD ArchivedUtc DATETIME2 NULL;
GO

-- Relational findings (dual-write with FindingsJson; typed payload only in FindingRecords.PayloadJson).
IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingRecords
    (
        FindingRecordId     UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_FindingRecords PRIMARY KEY,
        FindingsSnapshotId  UNIQUEIDENTIFIER NOT NULL,
        SortOrder           INT NOT NULL,
        FindingId           NVARCHAR(200) NOT NULL,
        FindingSchemaVersion INT NOT NULL,
        FindingType         NVARCHAR(200) NOT NULL,
        Category            NVARCHAR(200) NOT NULL,
        EngineType          NVARCHAR(200) NOT NULL,
        Severity            NVARCHAR(50) NOT NULL,
        Title               NVARCHAR(1000) NOT NULL,
        Rationale           NVARCHAR(MAX) NOT NULL,
        PayloadType         NVARCHAR(256) NULL,
        PayloadJson         NVARCHAR(MAX) NULL,
        CONSTRAINT FK_FindingRecords_FindingsSnapshots FOREIGN KEY (FindingsSnapshotId)
            REFERENCES dbo.FindingsSnapshots (FindingsSnapshotId) ON DELETE CASCADE,
        CONSTRAINT UQ_FindingRecords_Snapshot_Sort UNIQUE (FindingsSnapshotId, SortOrder)
    );

    CREATE NONCLUSTERED INDEX IX_FindingRecords_FindingsSnapshotId
        ON dbo.FindingRecords (FindingsSnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.FindingRelatedNodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingRelatedNodes
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        NodeId          NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_FindingRelatedNodes PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingRelatedNodes_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingRelatedNodes_Record
        ON dbo.FindingRelatedNodes (FindingRecordId);
END;
GO

IF OBJECT_ID(N'dbo.FindingRecommendedActions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingRecommendedActions
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        ActionText      NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingRecommendedActions PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingRecommendedActions_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingRecommendedActions_Record
        ON dbo.FindingRecommendedActions (FindingRecordId);
END;
GO

IF OBJECT_ID(N'dbo.FindingProperties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingProperties
    (
        FindingRecordId    UNIQUEIDENTIFIER NOT NULL,
        PropertySortOrder  INT NOT NULL,
        PropertyKey        NVARCHAR(200) NOT NULL,
        PropertyValue      NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingProperties PRIMARY KEY (FindingRecordId, PropertySortOrder),
        CONSTRAINT FK_FindingProperties_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingProperties_Record
        ON dbo.FindingProperties (FindingRecordId);
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceGraphNodesExamined', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceGraphNodesExamined
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        NodeId          NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_FindingTraceGraphNodesExamined PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingTraceGraphNodesExamined_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingTraceGraphNodesExamined_Record
        ON dbo.FindingTraceGraphNodesExamined (FindingRecordId);
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceRulesApplied', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceRulesApplied
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        RuleText        NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingTraceRulesApplied PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingTraceRulesApplied_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingTraceRulesApplied_Record
        ON dbo.FindingTraceRulesApplied (FindingRecordId);
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceDecisionsTaken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceDecisionsTaken
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        DecisionText    NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingTraceDecisionsTaken PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingTraceDecisionsTaken_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingTraceDecisionsTaken_Record
        ON dbo.FindingTraceDecisionsTaken (FindingRecordId);
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceAlternativePaths', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceAlternativePaths
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        PathText        NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingTraceAlternativePaths PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingTraceAlternativePaths_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingTraceAlternativePaths_Record
        ON dbo.FindingTraceAlternativePaths (FindingRecordId);
END;
GO

IF OBJECT_ID(N'dbo.FindingTraceNotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingTraceNotes
    (
        FindingRecordId UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        NoteText        NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingTraceNotes PRIMARY KEY (FindingRecordId, SortOrder),
        CONSTRAINT FK_FindingTraceNotes_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingTraceNotes_Record
        ON dbo.FindingTraceNotes (FindingRecordId);
END;
GO

IF OBJECT_ID('dbo.DecisioningTraces', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DecisioningTraces
    (
        DecisionTraceId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        RuleSetId NVARCHAR(200) NOT NULL,
        RuleSetVersion NVARCHAR(50) NOT NULL,
        RuleSetHash NVARCHAR(128) NOT NULL,
        AppliedRuleIdsJson NVARCHAR(MAX) NOT NULL,
        AcceptedFindingIdsJson NVARCHAR(MAX) NOT NULL,
        RejectedFindingIdsJson NVARCHAR(MAX) NOT NULL,
        NotesJson NVARCHAR(MAX) NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        INDEX IX_DecisioningTraces_RunId NONCLUSTERED (RunId)
    );
END;
GO

/* Brownfield: soft-archive on dbo.DecisioningTraces (DbUp 067 parity; cascaded when dbo.Runs bulk-archive). */
IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.DecisioningTraces', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.DecisioningTraces ADD ArchivedUtc DATETIME2 NULL;
GO

IF OBJECT_ID('dbo.GoldenManifests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifests
    (
        ManifestId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ContextSnapshotId UNIQUEIDENTIFIER NOT NULL,
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL,
        FindingsSnapshotId UNIQUEIDENTIFIER NOT NULL,
        DecisionTraceId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ManifestHash NVARCHAR(128) NOT NULL,
        RuleSetId NVARCHAR(200) NOT NULL,
        RuleSetVersion NVARCHAR(50) NOT NULL,
        RuleSetHash NVARCHAR(128) NOT NULL,
        MetadataJson NVARCHAR(MAX) NOT NULL,
        RequirementsJson NVARCHAR(MAX) NOT NULL,
        TopologyJson NVARCHAR(MAX) NOT NULL,
        SecurityJson NVARCHAR(MAX) NOT NULL,
        ComplianceJson NVARCHAR(MAX) NOT NULL,
        CostJson NVARCHAR(MAX) NOT NULL,
        ConstraintsJson NVARCHAR(MAX) NOT NULL,
        UnresolvedIssuesJson NVARCHAR(MAX) NOT NULL,
        DecisionsJson NVARCHAR(MAX) NOT NULL,
        AssumptionsJson NVARCHAR(MAX) NOT NULL,
        WarningsJson NVARCHAR(MAX) NOT NULL,
        ProvenanceJson NVARCHAR(MAX) NOT NULL,
        ManifestPayloadBlobUri NVARCHAR(2000) NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        RowVersionStamp ROWVERSION,
        INDEX IX_GoldenManifests_RunId NONCLUSTERED (RunId)
    );
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GoldenManifests', N'RowVersionStamp') IS NULL
    ALTER TABLE dbo.GoldenManifests ADD RowVersionStamp ROWVERSION;
GO

/* Brownfield: soft-archive on dbo.GoldenManifests (DbUp 066 parity; cascaded when dbo.Runs bulk-archive). */
IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GoldenManifests', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.GoldenManifests ADD ArchivedUtc DATETIME2 NULL;
GO

-- Phase-1 relational slices for GoldenManifest (dual-write; other sections remain JSON on dbo.GoldenManifests).
IF OBJECT_ID(N'dbo.GoldenManifestAssumptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestAssumptions
    (
        ManifestId      UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        AssumptionText  NVARCHAR(MAX) NOT NULL,
        TenantId        UNIQUEIDENTIFIER NULL,
        WorkspaceId     UNIQUEIDENTIFIER NULL,
        ProjectId       UNIQUEIDENTIFIER NULL,
        CONSTRAINT PK_GoldenManifestAssumptions PRIMARY KEY (ManifestId, SortOrder),
        CONSTRAINT FK_GoldenManifestAssumptions_GoldenManifests FOREIGN KEY (ManifestId)
            REFERENCES dbo.GoldenManifests (ManifestId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GoldenManifestAssumptions_ManifestId
        ON dbo.GoldenManifestAssumptions (ManifestId);
END;
GO

/* Brownfield: RLS scope denormalization (DbUp 046 parity) on dbo.GoldenManifestAssumptions */
IF OBJECT_ID(N'dbo.GoldenManifestAssumptions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.GoldenManifestAssumptions', N'TenantId') IS NULL
        ALTER TABLE dbo.GoldenManifestAssumptions ADD TenantId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.GoldenManifestAssumptions', N'WorkspaceId') IS NULL
        ALTER TABLE dbo.GoldenManifestAssumptions ADD WorkspaceId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.GoldenManifestAssumptions', N'ProjectId') IS NULL
        ALTER TABLE dbo.GoldenManifestAssumptions ADD ProjectId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestWarnings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestWarnings
    (
        ManifestId   UNIQUEIDENTIFIER NOT NULL,
        SortOrder    INT NOT NULL,
        WarningText  NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_GoldenManifestWarnings PRIMARY KEY (ManifestId, SortOrder),
        CONSTRAINT FK_GoldenManifestWarnings_GoldenManifests FOREIGN KEY (ManifestId)
            REFERENCES dbo.GoldenManifests (ManifestId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GoldenManifestWarnings_ManifestId
        ON dbo.GoldenManifestWarnings (ManifestId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestDecisions
    (
        ManifestId       UNIQUEIDENTIFIER NOT NULL,
        SortOrder        INT NOT NULL,
        DecisionId       NVARCHAR(200) NOT NULL,
        Category         NVARCHAR(500) NOT NULL,
        Title            NVARCHAR(500) NOT NULL,
        SelectedOption   NVARCHAR(2000) NOT NULL,
        Rationale        NVARCHAR(MAX) NOT NULL,
        RawDecisionJson  NVARCHAR(MAX) NULL,
        CONSTRAINT PK_GoldenManifestDecisions PRIMARY KEY (ManifestId, SortOrder),
        CONSTRAINT UQ_GoldenManifestDecisions_DecisionId UNIQUE (ManifestId, DecisionId),
        CONSTRAINT FK_GoldenManifestDecisions_GoldenManifests FOREIGN KEY (ManifestId)
            REFERENCES dbo.GoldenManifests (ManifestId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GoldenManifestDecisions_ManifestId
        ON dbo.GoldenManifestDecisions (ManifestId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisionEvidenceLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestDecisionEvidenceLinks
    (
        ManifestId   UNIQUEIDENTIFIER NOT NULL,
        DecisionId   NVARCHAR(200) NOT NULL,
        SortOrder    INT NOT NULL,
        FindingId    NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_GoldenManifestDecisionEvidenceLinks PRIMARY KEY (ManifestId, DecisionId, SortOrder),
        CONSTRAINT FK_GoldenManifestDecisionEvidenceLinks_Decisions FOREIGN KEY (ManifestId, DecisionId)
            REFERENCES dbo.GoldenManifestDecisions (ManifestId, DecisionId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GoldenManifestDecisionEvidenceLinks_Manifest
        ON dbo.GoldenManifestDecisionEvidenceLinks (ManifestId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestDecisionNodeLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestDecisionNodeLinks
    (
        ManifestId   UNIQUEIDENTIFIER NOT NULL,
        DecisionId   NVARCHAR(200) NOT NULL,
        SortOrder    INT NOT NULL,
        NodeId       NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_GoldenManifestDecisionNodeLinks PRIMARY KEY (ManifestId, DecisionId, SortOrder),
        CONSTRAINT FK_GoldenManifestDecisionNodeLinks_Decisions FOREIGN KEY (ManifestId, DecisionId)
            REFERENCES dbo.GoldenManifestDecisions (ManifestId, DecisionId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GoldenManifestDecisionNodeLinks_Manifest
        ON dbo.GoldenManifestDecisionNodeLinks (ManifestId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceFindings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestProvenanceSourceFindings
    (
        ManifestId   UNIQUEIDENTIFIER NOT NULL,
        SortOrder    INT NOT NULL,
        FindingId    NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_GoldenManifestProvenanceSourceFindings PRIMARY KEY (ManifestId, SortOrder),
        CONSTRAINT FK_GoldenManifestProvenanceSourceFindings_GoldenManifests FOREIGN KEY (ManifestId)
            REFERENCES dbo.GoldenManifests (ManifestId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GoldenManifestProvenanceSourceFindings_Manifest
        ON dbo.GoldenManifestProvenanceSourceFindings (ManifestId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceSourceGraphNodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestProvenanceSourceGraphNodes
    (
        ManifestId   UNIQUEIDENTIFIER NOT NULL,
        SortOrder    INT NOT NULL,
        NodeId       NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_GoldenManifestProvenanceSourceGraphNodes PRIMARY KEY (ManifestId, SortOrder),
        CONSTRAINT FK_GoldenManifestProvenanceSourceGraphNodes_GoldenManifests FOREIGN KEY (ManifestId)
            REFERENCES dbo.GoldenManifests (ManifestId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GoldenManifestProvenanceSourceGraphNodes_Manifest
        ON dbo.GoldenManifestProvenanceSourceGraphNodes (ManifestId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifestProvenanceAppliedRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestProvenanceAppliedRules
    (
        ManifestId   UNIQUEIDENTIFIER NOT NULL,
        SortOrder    INT NOT NULL,
        RuleId       NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_GoldenManifestProvenanceAppliedRules PRIMARY KEY (ManifestId, SortOrder),
        CONSTRAINT FK_GoldenManifestProvenanceAppliedRules_GoldenManifests FOREIGN KEY (ManifestId)
            REFERENCES dbo.GoldenManifests (ManifestId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GoldenManifestProvenanceAppliedRules_Manifest
        ON dbo.GoldenManifestProvenanceAppliedRules (ManifestId);
END;
GO

IF OBJECT_ID('dbo.ArtifactBundles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundles
    (
        BundleId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ArtifactsJson NVARCHAR(MAX) NULL,
        TraceJson NVARCHAR(MAX) NULL,
        BundlePayloadBlobUri NVARCHAR(2000) NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        INDEX IX_ArtifactBundles_RunId NONCLUSTERED (RunId),
        INDEX IX_ArtifactBundles_ManifestId NONCLUSTERED (ManifestId)
    );
END;
GO

/* ArtifactBundles legacy JSON columns nullable (see Migrations/043_ArtifactBundles_LegacyJsonNullable.sql). */
IF OBJECT_ID(N'dbo.ArtifactBundles', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON c.object_id = t.object_id
        WHERE t.schema_id = SCHEMA_ID(N'dbo')
          AND t.name = N'ArtifactBundles'
          AND c.name = N'ArtifactsJson'
          AND c.is_nullable = 0)
        ALTER TABLE dbo.ArtifactBundles ALTER COLUMN ArtifactsJson NVARCHAR(MAX) NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON c.object_id = t.object_id
        WHERE t.schema_id = SCHEMA_ID(N'dbo')
          AND t.name = N'ArtifactBundles'
          AND c.name = N'TraceJson'
          AND c.is_nullable = 0)
        ALTER TABLE dbo.ArtifactBundles ALTER COLUMN TraceJson NVARCHAR(MAX) NULL;
END;
GO

/* Brownfield: soft-archive on dbo.ArtifactBundles (DbUp 073 parity; cascaded when dbo.Runs bulk-archive). */
IF OBJECT_ID(N'dbo.ArtifactBundles', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ArtifactBundles', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.ArtifactBundles ADD ArchivedUtc DATETIME2 NULL;
GO

-- Relational artifact bundle slices (dual-write with ArtifactsJson / TraceJson on dbo.ArtifactBundles).
IF OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleArtifacts
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        ArtifactId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ArtifactType NVARCHAR(500) NOT NULL,
        Name NVARCHAR(2000) NOT NULL,
        Format NVARCHAR(200) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        ContentHash NVARCHAR(128) NOT NULL,
        ContentBlobUri NVARCHAR(2000) NULL,
        CONSTRAINT PK_ArtifactBundleArtifacts PRIMARY KEY (BundleId, SortOrder),
        CONSTRAINT UQ_ArtifactBundleArtifacts_ArtifactId UNIQUE (BundleId, ArtifactId),
        CONSTRAINT FK_ArtifactBundleArtifacts_Bundles FOREIGN KEY (BundleId)
            REFERENCES dbo.ArtifactBundles (BundleId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleArtifacts_BundleId
        ON dbo.ArtifactBundleArtifacts (BundleId);
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifactMetadata', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleArtifactMetadata
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        ArtifactSortOrder INT NOT NULL,
        MetaSortOrder INT NOT NULL,
        MetaKey NVARCHAR(500) NOT NULL,
        MetaValue NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ArtifactBundleArtifactMetadata PRIMARY KEY (BundleId, ArtifactSortOrder, MetaSortOrder),
        CONSTRAINT FK_ArtifactBundleArtifactMetadata_Artifacts FOREIGN KEY (BundleId, ArtifactSortOrder)
            REFERENCES dbo.ArtifactBundleArtifacts (BundleId, SortOrder) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleArtifactMetadata_Bundle
        ON dbo.ArtifactBundleArtifactMetadata (BundleId);
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifactDecisionLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleArtifactDecisionLinks
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        ArtifactSortOrder INT NOT NULL,
        LinkSortOrder INT NOT NULL,
        DecisionId NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_ArtifactBundleArtifactDecisionLinks PRIMARY KEY (BundleId, ArtifactSortOrder, LinkSortOrder),
        CONSTRAINT FK_ArtifactBundleArtifactDecisionLinks_Artifacts FOREIGN KEY (BundleId, ArtifactSortOrder)
            REFERENCES dbo.ArtifactBundleArtifacts (BundleId, SortOrder) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleArtifactDecisionLinks_Bundle
        ON dbo.ArtifactBundleArtifactDecisionLinks (BundleId);
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceGenerators', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleTraceGenerators
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        GeneratorName NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_ArtifactBundleTraceGenerators PRIMARY KEY (BundleId, SortOrder),
        CONSTRAINT FK_ArtifactBundleTraceGenerators_Bundles FOREIGN KEY (BundleId)
            REFERENCES dbo.ArtifactBundles (BundleId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleTraceGenerators_BundleId
        ON dbo.ArtifactBundleTraceGenerators (BundleId);
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceDecisionLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleTraceDecisionLinks
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        DecisionId NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_ArtifactBundleTraceDecisionLinks PRIMARY KEY (BundleId, SortOrder),
        CONSTRAINT FK_ArtifactBundleTraceDecisionLinks_Bundles FOREIGN KEY (BundleId)
            REFERENCES dbo.ArtifactBundles (BundleId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleTraceDecisionLinks_BundleId
        ON dbo.ArtifactBundleTraceDecisionLinks (BundleId);
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleTraceNotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleTraceNotes
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        NoteText NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ArtifactBundleTraceNotes PRIMARY KEY (BundleId, SortOrder),
        CONSTRAINT FK_ArtifactBundleTraceNotes_Bundles FOREIGN KEY (BundleId)
            REFERENCES dbo.ArtifactBundles (BundleId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleTraceNotes_BundleId
        ON dbo.ArtifactBundleTraceNotes (BundleId);
END;
GO

/* -- Remove placeholder scope defaults from runtime-authoritative tables ----
   Inserts must supply TenantId, WorkspaceId, ProjectId (scope), and Runs.ScopeProjectId.
   Batches below drop legacy named defaults on existing databases (greenfield CREATE above has none).
*/
IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Runs') AND name = N'DF_Runs_TenantId')
        ALTER TABLE dbo.Runs DROP CONSTRAINT DF_Runs_TenantId;

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Runs') AND name = N'DF_Runs_WorkspaceId')
        ALTER TABLE dbo.Runs DROP CONSTRAINT DF_Runs_WorkspaceId;

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Runs') AND name = N'DF_Runs_ScopeProjectId')
        ALTER TABLE dbo.Runs DROP CONSTRAINT DF_Runs_ScopeProjectId;
END;
GO

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.DecisioningTraces') AND name = N'DF_DecisioningTraces_TenantId_Create')
        ALTER TABLE dbo.DecisioningTraces DROP CONSTRAINT DF_DecisioningTraces_TenantId_Create;

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.DecisioningTraces') AND name = N'DF_DecisioningTraces_WorkspaceId_Create')
        ALTER TABLE dbo.DecisioningTraces DROP CONSTRAINT DF_DecisioningTraces_WorkspaceId_Create;

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.DecisioningTraces') AND name = N'DF_DecisioningTraces_ProjectId_Create')
        ALTER TABLE dbo.DecisioningTraces DROP CONSTRAINT DF_DecisioningTraces_ProjectId_Create;
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.GoldenManifests') AND name = N'DF_GoldenManifests_TenantId')
        ALTER TABLE dbo.GoldenManifests DROP CONSTRAINT DF_GoldenManifests_TenantId;

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.GoldenManifests') AND name = N'DF_GoldenManifests_WorkspaceId')
        ALTER TABLE dbo.GoldenManifests DROP CONSTRAINT DF_GoldenManifests_WorkspaceId;

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.GoldenManifests') AND name = N'DF_GoldenManifests_ProjectId')
        ALTER TABLE dbo.GoldenManifests DROP CONSTRAINT DF_GoldenManifests_ProjectId;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundles', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ArtifactBundles') AND name = N'DF_ArtifactBundles_TenantId')
        ALTER TABLE dbo.ArtifactBundles DROP CONSTRAINT DF_ArtifactBundles_TenantId;

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ArtifactBundles') AND name = N'DF_ArtifactBundles_WorkspaceId')
        ALTER TABLE dbo.ArtifactBundles DROP CONSTRAINT DF_ArtifactBundles_WorkspaceId;

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ArtifactBundles') AND name = N'DF_ArtifactBundles_ProjectId')
        ALTER TABLE dbo.ArtifactBundles DROP CONSTRAINT DF_ArtifactBundles_ProjectId;
END;
GO

/* -- Critical FK hardening for canonical runtime chain ----
   Authority/decisioning tables (dbo.Runs … dbo.ArtifactBundles): enforces insert order
   and referential integrity for the runtime chain. ON DELETE omitted => NO ACTION (default).
   GoldenManifests.DecisionTraceId references dbo.DecisioningTraces (PK column DecisionTraceId).
*/
IF OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContextSnapshots_Runs_RunId')
        ALTER TABLE dbo.ContextSnapshots ADD CONSTRAINT FK_ContextSnapshots_Runs_RunId
            FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GraphSnapshots_ContextSnapshots_ContextSnapshotId')
        ALTER TABLE dbo.GraphSnapshots ADD CONSTRAINT FK_GraphSnapshots_ContextSnapshots_ContextSnapshotId
            FOREIGN KEY (ContextSnapshotId) REFERENCES dbo.ContextSnapshots (SnapshotId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GraphSnapshots_Runs_RunId')
        ALTER TABLE dbo.GraphSnapshots ADD CONSTRAINT FK_GraphSnapshots_Runs_RunId
            FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_Runs_RunId')
        ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_Runs_RunId
            FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_ContextSnapshots_ContextSnapshotId')
        ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_ContextSnapshots_ContextSnapshotId
            FOREIGN KEY (ContextSnapshotId) REFERENCES dbo.ContextSnapshots (SnapshotId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_GraphSnapshots_GraphSnapshotId')
        ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_GraphSnapshots_GraphSnapshotId
            FOREIGN KEY (GraphSnapshotId) REFERENCES dbo.GraphSnapshots (GraphSnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DecisioningTraces_Runs_RunId')
        ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT FK_DecisioningTraces_Runs_RunId
            FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifests_Runs_RunId')
        ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_Runs_RunId
            FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifests_ContextSnapshots_ContextSnapshotId')
        ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_ContextSnapshots_ContextSnapshotId
            FOREIGN KEY (ContextSnapshotId) REFERENCES dbo.ContextSnapshots (SnapshotId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifests_GraphSnapshots_GraphSnapshotId')
        ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_GraphSnapshots_GraphSnapshotId
            FOREIGN KEY (GraphSnapshotId) REFERENCES dbo.GraphSnapshots (GraphSnapshotId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifests_FindingsSnapshots_FindingsSnapshotId')
        ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_FindingsSnapshots_FindingsSnapshotId
            FOREIGN KEY (FindingsSnapshotId) REFERENCES dbo.FindingsSnapshots (FindingsSnapshotId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifests_DecisioningTraces_DecisionTraceId')
        ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_DecisioningTraces_DecisionTraceId
            FOREIGN KEY (DecisionTraceId) REFERENCES dbo.DecisioningTraces (DecisionTraceId);
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundles', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArtifactBundles_Runs_RunId')
        ALTER TABLE dbo.ArtifactBundles ADD CONSTRAINT FK_ArtifactBundles_Runs_RunId
            FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArtifactBundles_GoldenManifests_ManifestId')
        ALTER TABLE dbo.ArtifactBundles ADD CONSTRAINT FK_ArtifactBundles_GoldenManifests_ManifestId
            FOREIGN KEY (ManifestId) REFERENCES dbo.GoldenManifests (ManifestId);
END;
GO

/* -- Critical uniqueness hardening for canonical runtime chain ----
   One authority GoldenManifest per Run; one FindingsSnapshot per graph snapshot.
   Not enforced below (see TODO): GraphSnapshots per ContextSnapshotId may be one-to-many
   (SqlGraphSnapshotRepository.GetLatestByContextSnapshotIdAsync); ArtifactBundles per ManifestId
   may be one-to-many (SqlArtifactBundleRepository.GetByManifestIdAsync uses TOP 1 by CreatedUtc).
*/
IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_GoldenManifests_RunId'
          AND object_id = OBJECT_ID(N'dbo.GoldenManifests'))
        CREATE UNIQUE INDEX UX_GoldenManifests_RunId ON dbo.GoldenManifests (RunId);
END;
GO

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_FindingsSnapshots_GraphSnapshotId'
          AND object_id = OBJECT_ID(N'dbo.FindingsSnapshots'))
        CREATE UNIQUE INDEX UX_FindingsSnapshots_GraphSnapshotId ON dbo.FindingsSnapshots (GraphSnapshotId);
END;
GO

-- TODO: Repository pattern allows multiple graph snapshots per context (latest-by-date query).
--       Confirm 1:1 authority semantics before enabling:
-- CREATE UNIQUE INDEX UX_GraphSnapshots_ContextSnapshotId ON dbo.GraphSnapshots (ContextSnapshotId);

-- TODO: Repository pattern allows multiple artifact bundles per manifest (latest-by-date query).
--       Confirm 1:1 synthesis semantics before enabling:
-- CREATE UNIQUE INDEX UX_ArtifactBundles_ManifestId ON dbo.ArtifactBundles (ManifestId);

/* Append-only audit stream (no UPDATE/DELETE from application code). */
IF OBJECT_ID('dbo.AuditEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditEvents
    (
        EventId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OccurredUtc DATETIME2 NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        ActorUserId NVARCHAR(200) NOT NULL,
        ActorUserName NVARCHAR(200) NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NULL,
        ManifestId UNIQUEIDENTIFIER NULL,
        ArtifactId UNIQUEIDENTIFIER NULL,
        DataJson NVARCHAR(MAX) NOT NULL,
        CorrelationId NVARCHAR(200) NULL,
        INDEX IX_AuditEvents_Scope_OccurredUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, OccurredUtc DESC)
    );
END;
GO

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_AuditEvents_CorrelationId'
          AND object_id = OBJECT_ID(N'dbo.AuditEvents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditEvents_CorrelationId
        ON dbo.AuditEvents (CorrelationId)
        WHERE CorrelationId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_AuditEvents_RunId_OccurredUtc'
          AND object_id = OBJECT_ID(N'dbo.AuditEvents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditEvents_RunId_OccurredUtc
        ON dbo.AuditEvents (RunId, OccurredUtc DESC)
        WHERE RunId IS NOT NULL;
END;
GO

/* Append-only enforcement: see Migration 051. */
IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
   AND OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.AuditEvents')
          AND dp.permission_name = N'UPDATE'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
BEGIN
    DENY UPDATE ON dbo.AuditEvents TO [ArchLucidApp];
END;
GO

IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
   AND OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.AuditEvents')
          AND dp.permission_name = N'DELETE'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
BEGIN
    DENY DELETE ON dbo.AuditEvents TO [ArchLucidApp];
END;
GO

IF OBJECT_ID('dbo.ProvenanceSnapshots', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProvenanceSnapshots
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        GraphJson NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        INDEX IX_ProvenanceSnapshots_Scope_Run NONCLUSTERED (TenantId, WorkspaceId, ProjectId, RunId, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.ConversationThreads', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConversationThreads
    (
        ThreadId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NULL,
        BaseRunId UNIQUEIDENTIFIER NULL,
        TargetRunId UNIQUEIDENTIFIER NULL,
        Title NVARCHAR(300) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        LastUpdatedUtc DATETIME2 NOT NULL,
        ArchivedUtc DATETIME2 NULL,
        INDEX IX_ConversationThreads_Scope NONCLUSTERED (TenantId, WorkspaceId, ProjectId, LastUpdatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.ConversationMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConversationMessages
    (
        MessageId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ThreadId UNIQUEIDENTIFIER NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        MetadataJson NVARCHAR(MAX) NOT NULL,
        INDEX IX_ConversationMessages_ThreadId_CreatedUtc NONCLUSTERED (ThreadId, CreatedUtc ASC)
    );
END;
GO

IF OBJECT_ID('dbo.RecommendationRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecommendationRecords
    (
        RecommendationId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,

        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,

        RunId UNIQUEIDENTIFIER NOT NULL,
        ComparedToRunId UNIQUEIDENTIFIER NULL,

        Title NVARCHAR(500) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Rationale NVARCHAR(MAX) NOT NULL,
        SuggestedAction NVARCHAR(MAX) NOT NULL,
        Urgency NVARCHAR(50) NOT NULL,
        ExpectedImpact NVARCHAR(MAX) NOT NULL,
        PriorityScore INT NOT NULL,

        Status NVARCHAR(50) NOT NULL,

        CreatedUtc DATETIME2 NOT NULL,
        LastUpdatedUtc DATETIME2 NOT NULL,

        ReviewedByUserId NVARCHAR(200) NULL,
        ReviewedByUserName NVARCHAR(200) NULL,
        ReviewComment NVARCHAR(MAX) NULL,
        ResolutionRationale NVARCHAR(MAX) NULL,

        SupportingFindingIdsJson NVARCHAR(MAX) NOT NULL,
        SupportingDecisionIdsJson NVARCHAR(MAX) NOT NULL,
        SupportingArtifactIdsJson NVARCHAR(MAX) NOT NULL,
        RowVersionStamp ROWVERSION,
        INDEX IX_RecommendationRecords_Scope_Run NONCLUSTERED (TenantId, WorkspaceId, ProjectId, RunId, CreatedUtc DESC),
        INDEX IX_RecommendationRecords_Scope_Status NONCLUSTERED (TenantId, WorkspaceId, ProjectId, Status, LastUpdatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.RecommendationLearningProfiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecommendationLearningProfiles
    (
        ProfileId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        GeneratedUtc DATETIME2 NOT NULL,
        ProfileJson NVARCHAR(MAX) NOT NULL,
        INDEX IX_RecommendationLearningProfiles_Scope_GeneratedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, GeneratedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.AdvisoryScanSchedules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdvisoryScanSchedules
    (
        ScheduleId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        RunProjectSlug NVARCHAR(200) NOT NULL CONSTRAINT DF_AdvisoryScanSchedules_RunProjectSlug DEFAULT ('default'),
        Name NVARCHAR(300) NOT NULL,
        CronExpression NVARCHAR(100) NOT NULL,
        IsEnabled BIT NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        LastRunUtc DATETIME2 NULL,
        NextRunUtc DATETIME2 NULL,
        INDEX IX_AdvisoryScanSchedules_Scope_Enabled_NextRun NONCLUSTERED (TenantId, WorkspaceId, ProjectId, IsEnabled, NextRunUtc)
    );
END;
GO

IF OBJECT_ID('dbo.AdvisoryScanExecutions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdvisoryScanExecutions
    (
        ExecutionId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ScheduleId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        StartedUtc DATETIME2 NOT NULL,
        CompletedUtc DATETIME2 NULL,
        Status NVARCHAR(50) NOT NULL,
        ResultJson NVARCHAR(MAX) NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        INDEX IX_AdvisoryScanExecutions_Schedule_StartedUtc NONCLUSTERED (ScheduleId, StartedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.ArchitectureDigests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArchitectureDigests
    (
        DigestId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NULL,
        ComparedToRunId UNIQUEIDENTIFIER NULL,
        GeneratedUtc DATETIME2 NOT NULL,
        Title NVARCHAR(300) NOT NULL,
        Summary NVARCHAR(MAX) NOT NULL,
        ContentMarkdown NVARCHAR(MAX) NOT NULL,
        MetadataJson NVARCHAR(MAX) NOT NULL,
        ArchivedUtc DATETIME2 NULL,
        INDEX IX_ArchitectureDigests_Scope_GeneratedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, GeneratedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.DigestSubscriptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DigestSubscriptions
    (
        SubscriptionId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(300) NOT NULL,
        ChannelType NVARCHAR(100) NOT NULL,
        Destination NVARCHAR(1000) NOT NULL,
        IsEnabled BIT NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        LastDeliveredUtc DATETIME2 NULL,
        MetadataJson NVARCHAR(MAX) NOT NULL,
        INDEX IX_DigestSubscriptions_Scope_Enabled NONCLUSTERED (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.DigestDeliveryAttempts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DigestDeliveryAttempts
    (
        AttemptId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        DigestId UNIQUEIDENTIFIER NOT NULL,
        SubscriptionId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        AttemptedUtc DATETIME2 NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ChannelType NVARCHAR(100) NOT NULL,
        Destination NVARCHAR(1000) NOT NULL,
        INDEX IX_DigestDeliveryAttempts_DigestId_AttemptedUtc NONCLUSTERED (DigestId, AttemptedUtc DESC),
        INDEX IX_DigestDeliveryAttempts_SubscriptionId_AttemptedUtc NONCLUSTERED (SubscriptionId, AttemptedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.AlertRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlertRules
    (
        RuleId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AlertRules PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(300) NOT NULL,
        RuleType NVARCHAR(100) NOT NULL,
        Severity NVARCHAR(50) NOT NULL,
        ThresholdValue DECIMAL(18, 4) NOT NULL,
        IsEnabled BIT NOT NULL,
        TargetChannelType NVARCHAR(100) NOT NULL,
        MetadataJson NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        INDEX IX_AlertRules_Scope_Enabled NONCLUSTERED (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.AlertRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlertRecords
    (
        AlertId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AlertRecords PRIMARY KEY,
        RuleId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NULL,
        ComparedToRunId UNIQUEIDENTIFIER NULL,
        RecommendationId UNIQUEIDENTIFIER NULL,
        Title NVARCHAR(500) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Severity NVARCHAR(50) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        TriggerValue NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        LastUpdatedUtc DATETIME2 NULL,
        AcknowledgedByUserId NVARCHAR(200) NULL,
        AcknowledgedByUserName NVARCHAR(200) NULL,
        ResolutionComment NVARCHAR(MAX) NULL,
        DeduplicationKey NVARCHAR(500) NOT NULL,
        RowVersionStamp ROWVERSION,
        INDEX IX_AlertRecords_Scope_Status_CreatedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, Status, CreatedUtc DESC),
        INDEX IX_AlertRecords_DeduplicationKey NONCLUSTERED (DeduplicationKey)
    );
END;
GO

IF OBJECT_ID('dbo.AlertRoutingSubscriptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlertRoutingSubscriptions
    (
        RoutingSubscriptionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AlertRoutingSubscriptions PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(300) NOT NULL,
        ChannelType NVARCHAR(100) NOT NULL,
        Destination NVARCHAR(1000) NOT NULL,
        MinimumSeverity NVARCHAR(50) NOT NULL,
        IsEnabled BIT NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        LastDeliveredUtc DATETIME2 NULL,
        MetadataJson NVARCHAR(MAX) NOT NULL,
        INDEX IX_AlertRoutingSubscriptions_Scope_Enabled NONCLUSTERED (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.AlertDeliveryAttempts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlertDeliveryAttempts
    (
        AlertDeliveryAttemptId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AlertDeliveryAttempts PRIMARY KEY,
        AlertId UNIQUEIDENTIFIER NOT NULL,
        RoutingSubscriptionId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        AttemptedUtc DATETIME2 NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ChannelType NVARCHAR(100) NOT NULL,
        Destination NVARCHAR(1000) NOT NULL,
        RetryCount INT NOT NULL,
        INDEX IX_AlertDeliveryAttempts_AlertId_AttemptedUtc NONCLUSTERED (AlertId, AttemptedUtc DESC),
        INDEX IX_AlertDeliveryAttempts_RoutingSubscriptionId_AttemptedUtc NONCLUSTERED (RoutingSubscriptionId, AttemptedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.CompositeAlertRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompositeAlertRules
    (
        CompositeRuleId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompositeAlertRules PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(300) NOT NULL,
        Severity NVARCHAR(50) NOT NULL,
        [Operator] NVARCHAR(20) NOT NULL,
        IsEnabled BIT NOT NULL,
        SuppressionWindowMinutes INT NOT NULL,
        CooldownMinutes INT NOT NULL,
        ReopenDeltaThreshold DECIMAL(18, 4) NOT NULL,
        DedupeScope NVARCHAR(100) NOT NULL,
        TargetChannelType NVARCHAR(100) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        INDEX IX_CompositeAlertRules_Scope_Enabled NONCLUSTERED (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.CompositeAlertRuleConditions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompositeAlertRuleConditions
    (
        ConditionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompositeAlertRuleConditions PRIMARY KEY,
        CompositeRuleId UNIQUEIDENTIFIER NOT NULL,
        MetricType NVARCHAR(100) NOT NULL,
        [Operator] NVARCHAR(50) NOT NULL,
        ThresholdValue DECIMAL(18, 4) NOT NULL,
        INDEX IX_CompositeAlertRuleConditions_CompositeRuleId NONCLUSTERED (CompositeRuleId)
    );
END;
GO

IF OBJECT_ID('dbo.PolicyPacks', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PolicyPacks
    (
        PolicyPackId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PolicyPacks PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(300) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        PackType NVARCHAR(50) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ActivatedUtc DATETIME2 NULL,
        CurrentVersion NVARCHAR(50) NOT NULL,
        INDEX IX_PolicyPacks_Scope_Status NONCLUSTERED (TenantId, WorkspaceId, ProjectId, Status, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.PolicyPackVersions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PolicyPackVersions
    (
        PolicyPackVersionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PolicyPackVersions PRIMARY KEY,
        PolicyPackId UNIQUEIDENTIFIER NOT NULL,
        [Version] NVARCHAR(50) NOT NULL,
        ContentJson NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        IsPublished BIT NOT NULL,
        INDEX UQ_PolicyPackVersions_PolicyPackId_Version UNIQUE NONCLUSTERED (PolicyPackId, [Version])
    );
END;
GO

IF OBJECT_ID('dbo.PolicyPackAssignments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PolicyPackAssignments
    (
        AssignmentId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PolicyPackAssignments PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        PolicyPackId UNIQUEIDENTIFIER NOT NULL,
        PolicyPackVersion NVARCHAR(50) NOT NULL,
        IsEnabled BIT NOT NULL,
        ScopeLevel NVARCHAR(50) NOT NULL CONSTRAINT DF_PolicyPackAssignments_ScopeLevel_Create DEFAULT (N'Project'),
        IsPinned BIT NOT NULL CONSTRAINT DF_PolicyPackAssignments_IsPinned_Create DEFAULT (0),
        AssignedUtc DATETIME2 NOT NULL,
        ArchivedUtc DATETIME2 NULL,
        RowVersionStamp ROWVERSION,
        INDEX IX_PolicyPackAssignments_Scope_Enabled NONCLUSTERED (TenantId, WorkspaceId, ProjectId, IsEnabled, AssignedUtc DESC),
        INDEX IX_PolicyPackAssignments_ScopeLevel_AssignedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, ScopeLevel, AssignedUtc DESC)
    );
END;
GO

IF OBJECT_ID(N'dbo.PolicyPackChangeLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PolicyPackChangeLog
    (
        ChangeLogId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_PolicyPackChangeLog_ChangeLogId DEFAULT NEWSEQUENTIALID(),
        PolicyPackId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        ChangeType NVARCHAR(64) NOT NULL,
        ChangedBy NVARCHAR(256) NOT NULL,
        ChangedUtc DATETIME2(7) NOT NULL
            CONSTRAINT DF_PolicyPackChangeLog_ChangedUtc DEFAULT SYSUTCDATETIME(),
        PreviousValue NVARCHAR(MAX) NULL,
        NewValue NVARCHAR(MAX) NULL,
        SummaryText NVARCHAR(512) NULL,
        CONSTRAINT PK_PolicyPackChangeLog
            PRIMARY KEY CLUSTERED (ChangeLogId)
    );

    CREATE NONCLUSTERED INDEX IX_PolicyPackChangeLog_PackId_ChangedUtc
        ON dbo.PolicyPackChangeLog (PolicyPackId, ChangedUtc DESC);

    CREATE NONCLUSTERED INDEX IX_PolicyPackChangeLog_TenantId_ChangedUtc
        ON dbo.PolicyPackChangeLog (TenantId, ChangedUtc DESC);
END;
GO

IF OBJECT_ID(N'dbo.PolicyPackAssignments', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PolicyPackAssignments', N'RowVersionStamp') IS NULL
    ALTER TABLE dbo.PolicyPackAssignments ADD RowVersionStamp ROWVERSION;
GO

IF OBJECT_ID(N'dbo.PolicyPackAssignments', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PolicyPackAssignments', N'BlockCommitOnCritical') IS NULL
    ALTER TABLE dbo.PolicyPackAssignments ADD BlockCommitOnCritical BIT NOT NULL
        CONSTRAINT DF_PolicyPackAssignments_BlockCommitOnCritical_Create DEFAULT (0);
GO

IF OBJECT_ID(N'dbo.PolicyPackAssignments', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PolicyPackAssignments', N'BlockCommitMinimumSeverity') IS NULL
    ALTER TABLE dbo.PolicyPackAssignments ADD BlockCommitMinimumSeverity INT NULL;
GO

/* ---- DbUp 058 parity: SLA tracking on governance approval requests ---- */

IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceApprovalRequests', N'SlaDeadlineUtc') IS NULL
    ALTER TABLE dbo.GovernanceApprovalRequests ADD SlaDeadlineUtc DATETIME2 NULL;

IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceApprovalRequests', N'SlaBreachNotifiedUtc') IS NULL
    ALTER TABLE dbo.GovernanceApprovalRequests ADD SlaBreachNotifiedUtc DATETIME2 NULL;
GO

/* ---- DbUp 059 parity: SLA breach monitoring + blob upload failure indexes ---- */

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceApprovalRequests_PendingSlaBreached'
      AND object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceApprovalRequests_PendingSlaBreached
        ON dbo.GovernanceApprovalRequests (SlaDeadlineUtc ASC)
        INCLUDE (ApprovalRequestId, RunId, RequestedBy, Status)
        WHERE SlaDeadlineUtc IS NOT NULL AND SlaBreachNotifiedUtc IS NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceApprovalRequests_Status_RequestedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceApprovalRequests_Status_RequestedUtc
        ON dbo.GovernanceApprovalRequests (Status, RequestedUtc DESC)
        INCLUDE (RunId, ManifestVersion, SourceEnvironment, TargetEnvironment);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_AgentExecutionTraces_BlobUploadFailed'
      AND object_id = OBJECT_ID(N'dbo.AgentExecutionTraces'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AgentExecutionTraces_BlobUploadFailed
        ON dbo.AgentExecutionTraces (RunId, CreatedUtc DESC)
        WHERE BlobUploadFailed = 1;
END
GO

-- DbUp 065 parity: filtered index for traces where mandatory inline forensic fallback failed
IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'InlineFallbackFailed') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE name = N'IX_AgentExecutionTraces_InlineFallbackFailed'
         AND object_id = OBJECT_ID(N'dbo.AgentExecutionTraces'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AgentExecutionTraces_InlineFallbackFailed
        ON dbo.AgentExecutionTraces (RunId, CreatedUtc DESC)
        WHERE InlineFallbackFailed = 1;
END
GO

/* ---- DbUp 060 parity: broader query coverage indexes ---- */

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_AuditEvents_EventType_OccurredUtc'
      AND object_id = OBJECT_ID(N'dbo.AuditEvents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditEvents_EventType_OccurredUtc
        ON dbo.AuditEvents (TenantId, WorkspaceId, ProjectId, EventType, OccurredUtc DESC);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ConversationThreads_Scope_Active'
      AND object_id = OBJECT_ID(N'dbo.ConversationThreads'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ConversationThreads_Scope_Active
        ON dbo.ConversationThreads (TenantId, WorkspaceId, ProjectId, LastUpdatedUtc DESC)
        WHERE ArchivedUtc IS NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceEnvironmentActivations_RunId_ActivatedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceEnvironmentActivations_RunId_ActivatedUtc
        ON dbo.GovernanceEnvironmentActivations (RunId, ActivatedUtc DESC);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceEnvironmentActivations_Environment_ActivatedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceEnvironmentActivations_Environment_ActivatedUtc
        ON dbo.GovernanceEnvironmentActivations (Environment, ActivatedUtc DESC)
        INCLUDE (RunId, ManifestVersion, IsActive);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernancePromotionRecords_RunId_PromotedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernancePromotionRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernancePromotionRecords_RunId_PromotedUtc
        ON dbo.GovernancePromotionRecords (RunId, PromotedUtc DESC);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_RecommendationRecords_Scope_Run_Priority'
      AND object_id = OBJECT_ID(N'dbo.RecommendationRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RecommendationRecords_Scope_Run_Priority
        ON dbo.RecommendationRecords (TenantId, WorkspaceId, ProjectId, RunId, PriorityScore DESC, CreatedUtc DESC);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_RecommendationRecords_Scope_LastUpdatedUtc'
      AND object_id = OBJECT_ID(N'dbo.RecommendationRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RecommendationRecords_Scope_LastUpdatedUtc
        ON dbo.RecommendationRecords (TenantId, WorkspaceId, ProjectId, LastUpdatedUtc DESC);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Runs_ArchiveRetention'
      AND object_id = OBJECT_ID(N'dbo.Runs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Runs_ArchiveRetention
        ON dbo.Runs (CreatedUtc ASC)
        INCLUDE (TenantId, WorkspaceId, ScopeProjectId)
        WHERE ArchivedUtc IS NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PolicyPackAssignments_Scope_Active'
      AND object_id = OBJECT_ID(N'dbo.PolicyPackAssignments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PolicyPackAssignments_Scope_Active
        ON dbo.PolicyPackAssignments (TenantId, ScopeLevel, AssignedUtc DESC)
        INCLUDE (WorkspaceId, ProjectId, PolicyPackId, IsEnabled, BlockCommitOnCritical, BlockCommitMinimumSeverity)
        WHERE ArchivedUtc IS NULL;
END
GO

/* -- First-wave CHECK constraints (obvious status domains only) ----
   dbo.Runs.LegacyRunStatus (nullable) may carry stringified ArchitectureRunStatus when populated by the application.
   No dbo.RunQueue table in this schema.
   No dbo.RecommendationActions table — workflow status is dbo.RecommendationRecords.Status (RecommendationStatus).
   Second-wave policy/alert/urgency domains: migration **095** + trailing **`ArchLucid.sql`** block (PolicyPacks.Status, AlertDeliveryAttempts.Status, severities, RecommendationRecords.Urgency).
*/
IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_Status')
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT CK_RecommendationRecords_Status
        CHECK (Status IN (N'Proposed', N'Accepted', N'Rejected', N'Deferred', N'Implemented'));
GO

IF OBJECT_ID(N'dbo.AdvisoryScanExecutions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AdvisoryScanExecutions_Status')
    ALTER TABLE dbo.AdvisoryScanExecutions ADD CONSTRAINT CK_AdvisoryScanExecutions_Status
        CHECK (Status IN (N'Started', N'Completed', N'Failed'));
GO

IF OBJECT_ID(N'dbo.DigestDeliveryAttempts', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DigestDeliveryAttempts_Status')
    ALTER TABLE dbo.DigestDeliveryAttempts ADD CONSTRAINT CK_DigestDeliveryAttempts_Status
        CHECK (Status IN (N'Started', N'Succeeded', N'Failed'));
GO

IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AlertRecords_Status')
    ALTER TABLE dbo.AlertRecords ADD CONSTRAINT CK_AlertRecords_Status
        CHECK (Status IN (N'Open', N'Acknowledged', N'Resolved', N'Suppressed'));
GO

/* ---- DbUp 019–021 parity (post-bootstrap migrations; idempotent add for brownfield / reference) ---- */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RetrievalIndexingOutbox' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RetrievalIndexingOutbox
    (
        OutboxId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RetrievalIndexingOutbox PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ProcessedUtc DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX IX_RetrievalIndexingOutbox_Pending
        ON dbo.RetrievalIndexingOutbox (ProcessedUtc, CreatedUtc)
        WHERE ProcessedUtc IS NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IntegrationEventOutbox' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.IntegrationEventOutbox
    (
        OutboxId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_IntegrationEventOutbox PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NULL,
        EventType NVARCHAR(256) NOT NULL,
        MessageId NVARCHAR(128) NULL,
        PayloadUtf8 VARBINARY(MAX) NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ProcessedUtc DATETIME2 NULL,
        RetryCount INT NOT NULL CONSTRAINT DF_IntegrationEventOutbox_RetryCount DEFAULT (0),
        NextRetryUtc DATETIME2 NULL,
        LastErrorMessage NVARCHAR(2048) NULL,
        DeadLetteredUtc DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX IX_IntegrationEventOutbox_Pending
        ON dbo.IntegrationEventOutbox (ProcessedUtc, NextRetryUtc, CreatedUtc)
        WHERE ProcessedUtc IS NULL AND DeadLetteredUtc IS NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuthorityPipelineWorkOutbox' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AuthorityPipelineWorkOutbox
    (
        OutboxId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AuthorityPipelineWorkOutbox PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ProcessedUtc DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX IX_AuthorityPipelineWorkOutbox_Pending
        ON dbo.AuthorityPipelineWorkOutbox (ProcessedUtc, CreatedUtc)
        WHERE ProcessedUtc IS NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ArchitectureRunIdempotency' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ArchitectureRunIdempotency
    (
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        IdempotencyKeyHash VARBINARY(32) NOT NULL,
        RequestFingerprint VARBINARY(32) NOT NULL,
        RunId NVARCHAR(64) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        CONSTRAINT PK_ArchitectureRunIdempotency PRIMARY KEY (TenantId, WorkspaceId, ProjectId, IdempotencyKeyHash)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Runs_Scope_Project_CreatedUtc'
      AND object_id = OBJECT_ID(N'dbo.Runs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Runs_Scope_Project_CreatedUtc
        ON dbo.Runs (TenantId, WorkspaceId, ScopeProjectId, ProjectId, CreatedUtc DESC);
END;
GO

/* ---- DbUp 028 parity: soft archival flags (retention job sets ArchivedUtc; reads filter active rows) ---- */

IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.Runs', 'ArchivedUtc') IS NULL
    ALTER TABLE dbo.Runs ADD ArchivedUtc DATETIME2 NULL;

/* ---- DbUp 048 parity: lifecycle / request columns on dbo.Runs ---- */

IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'ArchitectureRequestId') IS NULL
    ALTER TABLE dbo.Runs ADD ArchitectureRequestId NVARCHAR(64) NULL;

IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'LegacyRunStatus') IS NULL
    ALTER TABLE dbo.Runs ADD LegacyRunStatus NVARCHAR(64) NULL;

IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'CompletedUtc') IS NULL
    ALTER TABLE dbo.Runs ADD CompletedUtc DATETIME2 NULL;

IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'CurrentManifestVersion') IS NULL
    ALTER TABLE dbo.Runs ADD CurrentManifestVersion NVARCHAR(128) NULL;

IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'OtelTraceId') IS NULL
    ALTER TABLE dbo.Runs ADD OtelTraceId NVARCHAR(64) NULL;

/* DbUp 061 parity: covering list index for dbo.Runs scope + CreatedUtc DESC (avoids key lookups into clustered PK under concurrent writes). */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Runs_Scope_CreatedUtc'
      AND object_id = OBJECT_ID(N'dbo.Runs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Runs_Scope_CreatedUtc
        ON dbo.Runs (TenantId, WorkspaceId, ScopeProjectId, CreatedUtc DESC)
        INCLUDE (
            RunId,
            ProjectId,
            Description,
            ContextSnapshotId,
            GraphSnapshotId,
            FindingsSnapshotId,
            GoldenManifestId,
            DecisionTraceId,
            ArtifactBundleId,
            ArchitectureRequestId,
            LegacyRunStatus,
            CompletedUtc,
            CurrentManifestVersion,
            OtelTraceId,
            ArchivedUtc)
        WHERE ArchivedUtc IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArchitectureDigests', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.ArchitectureDigests', 'ArchivedUtc') IS NULL
    ALTER TABLE dbo.ArchitectureDigests ADD ArchivedUtc DATETIME2 NULL;

IF OBJECT_ID(N'dbo.ConversationThreads', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.ConversationThreads', 'ArchivedUtc') IS NULL
    ALTER TABLE dbo.ConversationThreads ADD ArchivedUtc DATETIME2 NULL;

/* ---- DbUp 029 parity: policy pack assignment archival (excluded from effective governance lists) ---- */

IF OBJECT_ID(N'dbo.PolicyPackAssignments', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.PolicyPackAssignments', 'ArchivedUtc') IS NULL
    ALTER TABLE dbo.PolicyPackAssignments ADD ArchivedUtc DATETIME2 NULL;

/* RLS DDL moved to end of script (after all referenced tables exist); see "DbUp 036 parity" section. */

/* ---- DbUp 031 parity: product learning pilot signals (58R) ---- */

IF OBJECT_ID(N'dbo.ProductLearningPilotSignals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningPilotSignals
    (
        SignalId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductLearningPilotSignals PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        ArchitectureRunId NVARCHAR(64) NULL,
        AuthorityRunId UNIQUEIDENTIFIER NULL,
        ManifestVersion NVARCHAR(128) NULL,
        SubjectType NVARCHAR(64) NOT NULL,
        Disposition NVARCHAR(32) NOT NULL,
        PatternKey NVARCHAR(200) NULL,
        ArtifactHint NVARCHAR(512) NULL,
        CommentShort NVARCHAR(2000) NULL,
        DetailJson NVARCHAR(MAX) NULL,
        RecordedByUserId NVARCHAR(256) NULL,
        RecordedByDisplayName NVARCHAR(256) NULL,
        RecordedUtc DATETIME2 NOT NULL,
        TriageStatus NVARCHAR(32) NOT NULL CONSTRAINT DF_ProductLearningPilotSignals_TriageStatus DEFAULT (N'Open'),
        INDEX IX_ProductLearningPilotSignals_Scope_RecordedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, RecordedUtc DESC),
        INDEX IX_ProductLearningPilotSignals_Scope_Disposition NONCLUSTERED (TenantId, WorkspaceId, ProjectId, Disposition, RecordedUtc DESC),
        INDEX IX_ProductLearningPilotSignals_Scope_PatternKey_Filtered NONCLUSTERED (TenantId, WorkspaceId, ProjectId, PatternKey, RecordedUtc DESC)
            WHERE PatternKey IS NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningPilotSignals', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ProductLearningPilotSignals_Disposition')
    ALTER TABLE dbo.ProductLearningPilotSignals ADD CONSTRAINT CK_ProductLearningPilotSignals_Disposition
        CHECK (Disposition IN (N'Trusted', N'Rejected', N'Revised', N'NeedsFollowUp'));
GO

IF OBJECT_ID(N'dbo.ProductLearningPilotSignals', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ProductLearningPilotSignals_TriageStatus')
    ALTER TABLE dbo.ProductLearningPilotSignals ADD CONSTRAINT CK_ProductLearningPilotSignals_TriageStatus
        CHECK (TriageStatus IN (N'Open', N'Triaged', N'Backlog', N'Done', N'WontFix'));
GO

/* ---- DbUp 032 parity: learning-to-planning bridge (59R) ---- */

IF OBJECT_ID(N'dbo.ProductLearningImprovementThemes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningImprovementThemes
    (
        ThemeId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductLearningImprovementThemes PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        ThemeKey NVARCHAR(256) NOT NULL,
        SourceAggregateKey NVARCHAR(450) NULL,
        PatternKey NVARCHAR(200) NULL,
        Title NVARCHAR(512) NOT NULL,
        Summary NVARCHAR(MAX) NOT NULL,
        AffectedArtifactTypeOrWorkflowArea NVARCHAR(512) NOT NULL,
        SeverityBand NVARCHAR(32) NOT NULL,
        EvidenceSignalCount INT NOT NULL,
        DistinctRunCount INT NOT NULL,
        AverageTrustScore FLOAT NULL,
        DerivationRuleVersion NVARCHAR(64) NOT NULL,
        Status NVARCHAR(32) NOT NULL CONSTRAINT DF_ProductLearningImprovementThemes_Status DEFAULT (N'Proposed'),
        CreatedUtc DATETIME2 NOT NULL,
        CreatedByUserId NVARCHAR(256) NULL,
        CONSTRAINT UQ_ProductLearningImprovementThemes_Scope_ThemeKey UNIQUE (TenantId, WorkspaceId, ProjectId, ThemeKey),
        INDEX IX_ProductLearningImprovementThemes_Scope_CreatedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementThemes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ProductLearningImprovementThemes_Status')
    ALTER TABLE dbo.ProductLearningImprovementThemes ADD CONSTRAINT CK_ProductLearningImprovementThemes_Status
        CHECK (Status IN (N'Proposed', N'Accepted', N'Superseded', N'Archived'));
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningImprovementPlans
    (
        PlanId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductLearningImprovementPlans PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        ThemeId UNIQUEIDENTIFIER NOT NULL,
        Title NVARCHAR(512) NOT NULL,
        Summary NVARCHAR(MAX) NOT NULL,
        BoundedActionsJson NVARCHAR(MAX) NOT NULL,
        PriorityScore INT NOT NULL,
        PriorityExplanation NVARCHAR(MAX) NULL,
        Status NVARCHAR(32) NOT NULL CONSTRAINT DF_ProductLearningImprovementPlans_Status DEFAULT (N'Proposed'),
        CreatedUtc DATETIME2 NOT NULL,
        CreatedByUserId NVARCHAR(256) NULL,
        INDEX IX_ProductLearningImprovementPlans_Scope_CreatedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, CreatedUtc DESC),
        INDEX IX_ProductLearningImprovementPlans_ThemeId NONCLUSTERED (ThemeId),
        CONSTRAINT FK_ProductLearningImprovementPlans_Theme FOREIGN KEY (ThemeId)
            REFERENCES dbo.ProductLearningImprovementThemes (ThemeId)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlans', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ProductLearningImprovementPlans_Status')
    ALTER TABLE dbo.ProductLearningImprovementPlans ADD CONSTRAINT CK_ProductLearningImprovementPlans_Status
        CHECK (Status IN (N'Proposed', N'UnderReview', N'Approved', N'Rejected', N'Completed'));
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningImprovementPlanArchitectureRuns
    (
        PlanId UNIQUEIDENTIFIER NOT NULL,
        ArchitectureRunId NVARCHAR(64) NOT NULL,
        CONSTRAINT PK_ProductLearningImprovementPlanArchitectureRuns PRIMARY KEY (PlanId, ArchitectureRunId),
        CONSTRAINT FK_ProductLearningImprovementPlanArchitectureRuns_Plan FOREIGN KEY (PlanId)
            REFERENCES dbo.ProductLearningImprovementPlans (PlanId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ProductLearningImprovementPlanArchitectureRuns_PlanId
        ON dbo.ProductLearningImprovementPlanArchitectureRuns (PlanId);
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningImprovementPlanSignalLinks
    (
        PlanId UNIQUEIDENTIFIER NOT NULL,
        SignalId UNIQUEIDENTIFIER NOT NULL,
        TriageStatusSnapshot NVARCHAR(32) NULL,
        CONSTRAINT PK_ProductLearningImprovementPlanSignalLinks PRIMARY KEY (PlanId, SignalId),
        CONSTRAINT FK_ProductLearningImprovementPlanSignalLinks_Plan FOREIGN KEY (PlanId)
            REFERENCES dbo.ProductLearningImprovementPlans (PlanId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ProductLearningImprovementPlanSignalLinks_PlanId
        ON dbo.ProductLearningImprovementPlanSignalLinks (PlanId);
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningImprovementPlanSignalLinks_Signal')
    ALTER TABLE dbo.ProductLearningImprovementPlanSignalLinks ADD CONSTRAINT FK_ProductLearningImprovementPlanSignalLinks_Signal
        FOREIGN KEY (SignalId) REFERENCES dbo.ProductLearningPilotSignals (SignalId);
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanSignalLinks', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ProductLearningImprovementPlanSignalLinks_TriageSnapshot')
    ALTER TABLE dbo.ProductLearningImprovementPlanSignalLinks ADD CONSTRAINT CK_ProductLearningImprovementPlanSignalLinks_TriageSnapshot
        CHECK (
            TriageStatusSnapshot IS NULL OR TriageStatusSnapshot IN (N'Open', N'Triaged', N'Backlog', N'Done', N'WontFix')
        );
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLearningImprovementPlanArtifactLinks
    (
        LinkId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductLearningImprovementPlanArtifactLinks PRIMARY KEY,
        PlanId UNIQUEIDENTIFIER NOT NULL,
        AuthorityBundleId UNIQUEIDENTIFIER NULL,
        AuthorityArtifactSortOrder INT NULL,
        PilotArtifactHint NVARCHAR(512) NULL,
        CONSTRAINT FK_ProductLearningImprovementPlanArtifactLinks_Plan FOREIGN KEY (PlanId)
            REFERENCES dbo.ProductLearningImprovementPlans (PlanId) ON DELETE CASCADE,
        CONSTRAINT CK_ProductLearningImprovementPlanArtifactLinks_Target
            CHECK (
                (AuthorityBundleId IS NOT NULL AND AuthorityArtifactSortOrder IS NOT NULL)
                OR (PilotArtifactHint IS NOT NULL)
            )
    );

    CREATE NONCLUSTERED INDEX IX_ProductLearningImprovementPlanArtifactLinks_PlanId
        ON dbo.ProductLearningImprovementPlanArtifactLinks (PlanId);
END;
GO

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArtifactLinks', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningImprovementPlanArtifactLinks_BundleArtifact')
    ALTER TABLE dbo.ProductLearningImprovementPlanArtifactLinks ADD CONSTRAINT FK_ProductLearningImprovementPlanArtifactLinks_BundleArtifact
        FOREIGN KEY (AuthorityBundleId, AuthorityArtifactSortOrder) REFERENCES dbo.ArtifactBundleArtifacts (BundleId, SortOrder);
GO

/* ---- DbUp 033 parity: evolution simulation (60R) ---- */

IF OBJECT_ID(N'dbo.EvolutionCandidateChangeSets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EvolutionCandidateChangeSets
    (
        CandidateChangeSetId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EvolutionCandidateChangeSets PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        SourcePlanId UNIQUEIDENTIFIER NOT NULL,
        Status NVARCHAR(32) NOT NULL CONSTRAINT DF_EvolutionCandidateChangeSets_Status DEFAULT (N'Draft'),
        Title NVARCHAR(512) NOT NULL,
        Summary NVARCHAR(MAX) NOT NULL,
        PlanSnapshotJson NVARCHAR(MAX) NOT NULL,
        DerivationRuleVersion NVARCHAR(64) NOT NULL CONSTRAINT DF_EvolutionCandidateChangeSets_RuleVersion DEFAULT (N'60R-v1'),
        CreatedUtc DATETIME2 NOT NULL,
        CreatedByUserId NVARCHAR(256) NULL,
        INDEX IX_EvolutionCandidateChangeSets_Scope_CreatedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, CreatedUtc DESC),
        INDEX IX_EvolutionCandidateChangeSets_SourcePlanId NONCLUSTERED (SourcePlanId),
        CONSTRAINT FK_EvolutionCandidateChangeSets_Plan FOREIGN KEY (SourcePlanId)
            REFERENCES dbo.ProductLearningImprovementPlans (PlanId)
    );
END;
GO

IF OBJECT_ID(N'dbo.EvolutionCandidateChangeSets', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EvolutionCandidateChangeSets_Status')
    ALTER TABLE dbo.EvolutionCandidateChangeSets ADD CONSTRAINT CK_EvolutionCandidateChangeSets_Status
        CHECK (Status IN (N'Draft', N'Simulated', N'PendingHumanReview', N'Declined', N'Archived'));
GO

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EvolutionSimulationRuns
    (
        SimulationRunId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EvolutionSimulationRuns PRIMARY KEY,
        CandidateChangeSetId UNIQUEIDENTIFIER NOT NULL,
        BaselineArchitectureRunId NVARCHAR(64) NOT NULL,
        EvaluationMode NVARCHAR(64) NOT NULL,
        OutcomeJson NVARCHAR(MAX) NOT NULL,
        WarningsJson NVARCHAR(MAX) NULL,
        CompletedUtc DATETIME2 NOT NULL,
        IsShadowOnly BIT NOT NULL CONSTRAINT DF_EvolutionSimulationRuns_IsShadowOnly DEFAULT (1),
        CONSTRAINT FK_EvolutionSimulationRuns_Candidate FOREIGN KEY (CandidateChangeSetId)
            REFERENCES dbo.EvolutionCandidateChangeSets (CandidateChangeSetId) ON DELETE CASCADE,
        INDEX IX_EvolutionSimulationRuns_CandidateId NONCLUSTERED (CandidateChangeSetId)
    );
END;
GO

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EvolutionSimulationRuns_EvaluationMode')
    ALTER TABLE dbo.EvolutionSimulationRuns ADD CONSTRAINT CK_EvolutionSimulationRuns_EvaluationMode
        CHECK (EvaluationMode IN (N'ReadOnlyArchitectureAnalysis'));
GO

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EvolutionSimulationRuns_ShadowOnly')
    ALTER TABLE dbo.EvolutionSimulationRuns ADD CONSTRAINT CK_EvolutionSimulationRuns_ShadowOnly
        CHECK (IsShadowOnly = 1);
GO

/* ---- Large artifact blob pointers (see Migrations/034_LargeArtifactBlobPointers.sql) ---- */
IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GoldenManifests', N'ManifestPayloadBlobUri') IS NULL
    ALTER TABLE dbo.GoldenManifests ADD ManifestPayloadBlobUri NVARCHAR(2000) NULL;
GO

IF OBJECT_ID(N'dbo.ArtifactBundles', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ArtifactBundles', N'BundlePayloadBlobUri') IS NULL
    ALTER TABLE dbo.ArtifactBundles ADD BundlePayloadBlobUri NVARCHAR(2000) NULL;
GO

IF OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ArtifactBundleArtifacts', N'ContentBlobUri') IS NULL
    ALTER TABLE dbo.ArtifactBundleArtifacts ADD ContentBlobUri NVARCHAR(2000) NULL;
GO

/* ---- Durable background export jobs (queue + worker) ---- */
IF OBJECT_ID(N'dbo.BackgroundJobs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BackgroundJobs
    (
        JobId           NVARCHAR(32)  NOT NULL CONSTRAINT PK_BackgroundJobs PRIMARY KEY,
        WorkUnitJson    NVARCHAR(MAX) NOT NULL,
        State           NVARCHAR(16)  NOT NULL,
        CreatedUtc      DATETIME2     NOT NULL,
        StartedUtc      DATETIME2     NULL,
        CompletedUtc    DATETIME2     NULL,
        Error           NVARCHAR(MAX) NULL,
        FileName        NVARCHAR(512) NULL,
        ContentType     NVARCHAR(256) NULL,
        RetryCount      INT           NOT NULL CONSTRAINT DF_BackgroundJobs_RetryCount DEFAULT (0),
        MaxRetries      INT           NOT NULL CONSTRAINT DF_BackgroundJobs_MaxRetries DEFAULT (0),
        ResultBlobName  NVARCHAR(1024) NULL,
        RowVersionStamp ROWVERSION,
        INDEX IX_BackgroundJobs_State_CreatedUtc NONCLUSTERED (State, CreatedUtc)
    );
END;
GO

IF OBJECT_ID(N'dbo.BackgroundJobs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_BackgroundJobs_State')
    ALTER TABLE dbo.BackgroundJobs ADD CONSTRAINT CK_BackgroundJobs_State
        CHECK (State IN (N'Pending', N'Running', N'Succeeded', N'Failed'));
GO

/* ---- Host leader leases (singleton hosted services; see Migrations/035_HostLeaderLeases.sql) ---- */
IF OBJECT_ID(N'dbo.HostLeaderLeases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HostLeaderLeases
    (
        LeaseName        NVARCHAR(128) NOT NULL CONSTRAINT PK_HostLeaderLeases PRIMARY KEY,
        HolderInstanceId NVARCHAR(256) NOT NULL,
        LeaseExpiresUtc  DATETIME2     NOT NULL
    );
END;
GO

/* ---- DbUp 036 parity: RLS on scope-keyed authority tables (STATE = OFF; see docs/security/MULTI_TENANT_RLS.md) ---- */

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'rls')
    EXEC(N'CREATE SCHEMA rls');
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'RunsScopeFilter')
    DROP SECURITY POLICY rls.RunsScopeFilter;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
    DROP SECURITY POLICY rls.ArchiforgeTenantScope;
GO

IF OBJECT_ID(N'rls.runs_scope_predicate', N'IF') IS NOT NULL
    DROP FUNCTION rls.runs_scope_predicate;
GO

IF OBJECT_ID(N'rls.archiforge_scope_predicate', N'IF') IS NOT NULL
    DROP FUNCTION rls.archiforge_scope_predicate;
GO

CREATE FUNCTION rls.archiforge_scope_predicate(
    @TenantId uniqueidentifier,
    @WorkspaceId uniqueidentifier,
    @ProjectScopeId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS access_granted
    WHERE ISNULL(TRY_CONVERT(int, SESSION_CONTEXT(N'af_rls_bypass')), 0) = 1
       OR (
            @TenantId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'af_tenant_id'))
        AND @WorkspaceId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'af_workspace_id'))
        AND @ProjectScopeId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'af_project_id'))
       )
);
GO

CREATE SECURITY POLICY rls.ArchiforgeTenantScope
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.Runs,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DecisioningTraces,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifests,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshots,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingsSnapshots,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestAssumptions,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundles,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuditEvents,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProvenanceSnapshots,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationThreads,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationRecords,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationLearningProfiles,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanSchedules,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanExecutions,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureDigests,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestSubscriptions,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestDeliveryAttempts,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRules,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRecords,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRoutingSubscriptions,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertDeliveryAttempts,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRules,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPacks,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackAssignments,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RetrievalIndexingOutbox,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.IntegrationEventOutbox,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuthorityPipelineWorkOutbox,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureRunIdempotency,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningPilotSignals,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementThemes,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlans,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionCandidateChangeSets,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.Runs AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.Runs AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.Runs BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DecisioningTraces AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DecisioningTraces AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DecisioningTraces BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifests AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifests AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifests BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshots AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshots AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshots BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingsSnapshots AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingsSnapshots AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingsSnapshots BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestAssumptions AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestAssumptions AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestAssumptions BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundles AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundles AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArtifactBundles BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuditEvents AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuditEvents AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuditEvents BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProvenanceSnapshots AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProvenanceSnapshots AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProvenanceSnapshots BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationThreads AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationThreads AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConversationThreads BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationRecords AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationRecords AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationRecords BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationLearningProfiles AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationLearningProfiles AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RecommendationLearningProfiles BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanSchedules AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanSchedules AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanSchedules BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanExecutions AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanExecutions AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AdvisoryScanExecutions BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureDigests AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureDigests AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureDigests BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestSubscriptions AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestSubscriptions AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestSubscriptions BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestDeliveryAttempts AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestDeliveryAttempts AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.DigestDeliveryAttempts BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRules AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRules AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRules BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRecords AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRecords AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRecords BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRoutingSubscriptions AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRoutingSubscriptions AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertRoutingSubscriptions BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertDeliveryAttempts AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertDeliveryAttempts AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AlertDeliveryAttempts BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRules AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRules AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.CompositeAlertRules BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPacks AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPacks AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPacks BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackAssignments AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackAssignments AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackAssignments BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RetrievalIndexingOutbox AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RetrievalIndexingOutbox AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.RetrievalIndexingOutbox BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.IntegrationEventOutbox AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.IntegrationEventOutbox AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.IntegrationEventOutbox BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuthorityPipelineWorkOutbox AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuthorityPipelineWorkOutbox AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.AuthorityPipelineWorkOutbox BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureRunIdempotency AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureRunIdempotency AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureRunIdempotency BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningPilotSignals AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningPilotSignals AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningPilotSignals BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementThemes AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementThemes AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementThemes BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlans AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlans AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlans BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionCandidateChangeSets AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionCandidateChangeSets AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionCandidateChangeSets BEFORE DELETE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog AFTER INSERT,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog AFTER UPDATE,
    ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog BEFORE DELETE
    WITH (STATE = OFF);
GO

/* ---- Tenant registry + usage metering (DbUp 069–070 parity; greenfield) ---- */

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Tenants PRIMARY KEY,
        Name             NVARCHAR(200)    NOT NULL,
        Slug             NVARCHAR(100)    NOT NULL,
        Tier             NVARCHAR(32)     NOT NULL CONSTRAINT DF_Tenants_Tier DEFAULT N'Standard',
        CreatedUtc       DATETIMEOFFSET   NOT NULL CONSTRAINT DF_Tenants_CreatedUtc2 DEFAULT SYSUTCDATETIME(),
        SuspendedUtc     DATETIMEOFFSET   NULL,
        EntraTenantId    UNIQUEIDENTIFIER NULL,
        CONSTRAINT UQ_Tenants_Slug2 UNIQUE (Slug)
    );
END;
GO

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Tenants', N'EntraTenantId') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD EntraTenantId UNIQUEIDENTIFIER NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Tenants_EntraTenantId'
      AND object_id = OBJECT_ID(N'dbo.Tenants', N'U')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Tenants_EntraTenantId
        ON dbo.Tenants (EntraTenantId)
        WHERE EntraTenantId IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Tenants', N'TrialStartUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD
        TrialStartUtc      DATETIMEOFFSET   NULL,
        TrialExpiresUtc    DATETIMEOFFSET   NULL,
        TrialRunsLimit     INT              NULL,
        TrialRunsUsed      INT              NOT NULL CONSTRAINT DF_Tenants_TrialRunsUsed DEFAULT 0,
        TrialSeatsLimit    INT              NULL,
        TrialSeatsUsed     INT              NOT NULL CONSTRAINT DF_Tenants_TrialSeatsUsed DEFAULT 1,
        TrialStatus        NVARCHAR(32)     NULL,
        TrialSampleRunId   UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Tenants', N'TrialArchitecturePreseedEnqueuedUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD TrialArchitecturePreseedEnqueuedUtc DATETIMEOFFSET NULL;
END;
GO

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Tenants', N'TrialWelcomeRunId') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD TrialWelcomeRunId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Tenants', N'BaselineReviewCycleHours') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD
        BaselineReviewCycleHours DECIMAL(9,2) NULL,
        BaselineReviewCycleSource NVARCHAR(256) NULL,
        BaselineReviewCycleCapturedUtc DATETIMEOFFSET(7) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Tenants', N'BaselineReviewCycleHours') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_Tenants_BaselineReviewCycleHours_Positive'
         AND parent_object_id = OBJECT_ID(N'dbo.Tenants', N'U'))
BEGIN
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_BaselineReviewCycleHours_Positive
        CHECK (BaselineReviewCycleHours IS NULL OR BaselineReviewCycleHours > 0);
END;
GO

IF OBJECT_ID(N'dbo.TenantTrialSeatOccupants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantTrialSeatOccupants
    (
        TenantId       UNIQUEIDENTIFIER NOT NULL,
        PrincipalKey   NVARCHAR(450)    NOT NULL,
        CreatedUtc     DATETIMEOFFSET   NOT NULL CONSTRAINT DF_TenantTrialSeatOccupants_CreatedUtc2 DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_TenantTrialSeatOccupants2 PRIMARY KEY (TenantId, PrincipalKey),
        CONSTRAINT FK_TenantTrialSeatOccupants_Tenants2 FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );

    CREATE NONCLUSTERED INDEX IX_TenantTrialSeatOccupants_TenantId2
        ON dbo.TenantTrialSeatOccupants (TenantId);
END;
GO

-- 077: Trial local identity users (email/password; see docs/security/TRIAL_AUTH.md).
IF OBJECT_ID(N'dbo.IdentityUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IdentityUsers
    (
        Id                            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_IdentityUsers2 PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        NormalizedEmail               NVARCHAR(256)    NOT NULL,
        Email                         NVARCHAR(256)    NOT NULL,
        PasswordHash                  NVARCHAR(500)    NOT NULL,
        SecurityStamp                 NVARCHAR(256)    NOT NULL,
        ConcurrencyStamp              NVARCHAR(256)    NOT NULL,
        EmailConfirmed                BIT              NOT NULL CONSTRAINT DF_IdentityUsers_EmailConfirmed2 DEFAULT (0),
        EmailVerifiedUtc              DATETIMEOFFSET   NULL,
        LockoutEnd                    DATETIMEOFFSET   NULL,
        LockoutEnabled                BIT              NOT NULL CONSTRAINT DF_IdentityUsers_LockoutEnabled2 DEFAULT (1),
        AccessFailedCount             INT              NOT NULL CONSTRAINT DF_IdentityUsers_AccessFailedCount2 DEFAULT (0),
        EmailConfirmationTokenHash    NVARCHAR(128)    NULL,
        EmailConfirmationExpiresUtc   DATETIMEOFFSET   NULL,
        CreatedUtc                    DATETIMEOFFSET   NOT NULL CONSTRAINT DF_IdentityUsers_CreatedUtc2 DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_IdentityUsers_NormalizedEmail2 ON dbo.IdentityUsers (NormalizedEmail);
END;
GO

IF OBJECT_ID(N'dbo.TenantWorkspaces', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantWorkspaces
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TenantWorkspaces PRIMARY KEY,
        TenantId          UNIQUEIDENTIFIER NOT NULL,
        Name              NVARCHAR(200)    NOT NULL,
        DefaultProjectId  UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc        DATETIMEOFFSET   NOT NULL CONSTRAINT DF_TenantWorkspaces_CreatedUtc2 DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_TenantWorkspaces_Tenants2 FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );

    CREATE NONCLUSTERED INDEX IX_TenantWorkspaces_TenantId2 ON dbo.TenantWorkspaces (TenantId);
END;
GO

IF OBJECT_ID(N'dbo.UsageEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsageEvents
    (
        Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_UsageEvents_Id2 DEFAULT NEWSEQUENTIALID(),
        TenantId       UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId    UNIQUEIDENTIFIER NOT NULL,
        ProjectId      UNIQUEIDENTIFIER NOT NULL,
        Kind           NVARCHAR(64)     NOT NULL,
        Quantity       BIGINT           NOT NULL,
        RecordedUtc    DATETIMEOFFSET   NOT NULL CONSTRAINT DF_UsageEvents_RecordedUtc2 DEFAULT SYSUTCDATETIME(),
        CorrelationId  NVARCHAR(256)    NULL,
        CONSTRAINT PK_UsageEvents2 PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_UsageEvents_Quantity2 CHECK (Quantity >= 0)
    );

    CREATE NONCLUSTERED INDEX IX_UsageEvents_TenantRecorded2 ON dbo.UsageEvents (TenantId, RecordedUtc);
    CREATE NONCLUSTERED INDEX IX_UsageEvents_KindRecorded2 ON dbo.UsageEvents (Kind, RecordedUtc);
END;
GO

/* 076: SentEmails idempotency ledger (transactional email). */
IF OBJECT_ID(N'dbo.SentEmails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SentEmails
    (
        IdempotencyKey     NVARCHAR(450)    NOT NULL CONSTRAINT PK_SentEmails2 PRIMARY KEY,
        TenantId           UNIQUEIDENTIFIER NOT NULL,
        TemplateId         NVARCHAR(128)    NOT NULL,
        SentUtc            DATETIMEOFFSET   NOT NULL CONSTRAINT DF_SentEmails_SentUtc2 DEFAULT SYSUTCDATETIME(),
        Provider           NVARCHAR(64)     NOT NULL,
        ProviderMessageId  NVARCHAR(256)    NULL
    );

    CREATE NONCLUSTERED INDEX IX_SentEmails_TenantTemplate2
        ON dbo.SentEmails (TenantId, TemplateId);
END;
GO

/* 078: Billing subscriptions + webhook idempotency (see Migrations/078_BillingSubscriptions.sql). */
IF OBJECT_ID(N'dbo.BillingSubscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BillingSubscriptions
    (
        TenantId               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_BillingSubscriptions2 PRIMARY KEY,
        WorkspaceId            UNIQUEIDENTIFIER NOT NULL,
        ProjectId              UNIQUEIDENTIFIER NOT NULL,
        Provider               NVARCHAR(64)     NOT NULL,
        ProviderSubscriptionId NVARCHAR(256)    NOT NULL CONSTRAINT DF_BillingSubscriptions_ProviderSubscriptionId2 DEFAULT N'',
        Tier                   NVARCHAR(32)     NOT NULL,
        SeatsPurchased         INT              NOT NULL CONSTRAINT DF_BillingSubscriptions_SeatsPurchased2 DEFAULT (0),
        WorkspacesPurchased    INT              NOT NULL CONSTRAINT DF_BillingSubscriptions_WorkspacesPurchased2 DEFAULT (0),
        Status                 NVARCHAR(32)     NOT NULL,
        ActivatedUtc           DATETIMEOFFSET   NULL,
        CanceledUtc            DATETIMEOFFSET   NULL,
        RawWebhookJson         NVARCHAR(MAX)    NULL,
        CreatedUtc             DATETIMEOFFSET   NOT NULL CONSTRAINT DF_BillingSubscriptions_CreatedUtc2 DEFAULT (SYSUTCDATETIME()),
        UpdatedUtc             DATETIMEOFFSET   NOT NULL CONSTRAINT DF_BillingSubscriptions_UpdatedUtc2 DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_BillingSubscriptions_Tenants2 FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT CK_BillingSubscriptions_Status2 CHECK (Status IN (N'Pending', N'Active', N'Suspended', N'Canceled'))
    );

    CREATE NONCLUSTERED INDEX IX_BillingSubscriptions_ProviderSession2
        ON dbo.BillingSubscriptions (Provider, ProviderSubscriptionId);
END;
GO

IF OBJECT_ID(N'dbo.BillingWebhookEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BillingWebhookEvents
    (
        EventId      NVARCHAR(128)  NOT NULL CONSTRAINT PK_BillingWebhookEvents2 PRIMARY KEY,
        Provider     NVARCHAR(64)    NOT NULL,
        EventType    NVARCHAR(128)   NOT NULL,
        PayloadJson  NVARCHAR(MAX)   NOT NULL,
        ReceivedUtc  DATETIMEOFFSET  NOT NULL CONSTRAINT DF_BillingWebhookEvents_ReceivedUtc2 DEFAULT (SYSUTCDATETIME()),
        ProcessedUtc DATETIMEOFFSET  NULL,
        ResultStatus NVARCHAR(64)    NULL
    );

    CREATE NONCLUSTERED INDEX IX_BillingWebhookEvents_ProviderReceived2
        ON dbo.BillingWebhookEvents (Provider, ReceivedUtc);
END;
GO

/* 086: Marketplace ChangePlan / ChangeQuantity (see Migrations/086_Billing_MarketplaceChangePlanQuantity.sql). */
CREATE OR ALTER PROCEDURE dbo.sp_Billing_ChangePlan
    @TenantId uniqueidentifier,
    @Tier nvarchar(32),
    @RawWebhookJson nvarchar(max)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.BillingSubscriptions
    SET Tier = @Tier,
        RawWebhookJson = @RawWebhookJson,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Billing_ChangeQuantity
    @TenantId uniqueidentifier,
    @SeatsPurchased int,
    @RawWebhookJson nvarchar(max)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.BillingSubscriptions
    SET SeatsPurchased = @SeatsPurchased,
        RawWebhookJson = @RawWebhookJson,
        UpdatedUtc = SYSUTCDATETIME()
    WHERE TenantId = @TenantId;
END;
GO

IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.sp_Billing_ChangePlan', N'P') IS NOT NULL
        GRANT EXECUTE ON OBJECT::dbo.sp_Billing_ChangePlan TO [ArchLucidApp];

    IF OBJECT_ID(N'dbo.sp_Billing_ChangeQuantity', N'P') IS NOT NULL
        GRANT EXECUTE ON OBJECT::dbo.sp_Billing_ChangeQuantity TO [ArchLucidApp];
END;
GO

/* 079: Trial lifecycle transition log (see Migrations/079_TenantLifecycleTransitions.sql). */
IF OBJECT_ID(N'dbo.TenantLifecycleTransitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantLifecycleTransitions
    (
        TransitionId BIGINT            NOT NULL IDENTITY(1, 1) CONSTRAINT PK_TenantLifecycleTransitions2 PRIMARY KEY,
        TenantId       UNIQUEIDENTIFIER NOT NULL,
        FromStatus     NVARCHAR(32)     NOT NULL,
        ToStatus       NVARCHAR(32)     NOT NULL,
        OccurredUtc    DATETIMEOFFSET   NOT NULL CONSTRAINT DF_TenantLifecycleTransitions_OccurredUtc2 DEFAULT (SYSUTCDATETIME()),
        Reason         NVARCHAR(256)    NULL
    );

    CREATE NONCLUSTERED INDEX IX_TenantLifecycleTransitions_Tenant_OccurredUtc2
        ON dbo.TenantLifecycleTransitions (TenantId, OccurredUtc DESC);
END;
GO

/* 080: Enforce one row per (PolicyPackId, Version); see Migrations/080_PolicyPackVersions_UniquePackVersion.sql. */
IF OBJECT_ID(N'dbo.PolicyPackVersions', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'dbo.PolicyPackVersions')
         AND name = N'UQ_PolicyPackVersions_PolicyPackId_Version')
BEGIN
    ;WITH Ranked080 AS (
        SELECT
            PolicyPackVersionId,
            ROW_NUMBER() OVER (
                PARTITION BY PolicyPackId, [Version]
                ORDER BY CreatedUtc DESC, PolicyPackVersionId DESC) AS rn
        FROM dbo.PolicyPackVersions
    )
    DELETE v
    FROM dbo.PolicyPackVersions v
    INNER JOIN Ranked080 r ON r.PolicyPackVersionId = v.PolicyPackVersionId
    WHERE r.rn > 1;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PolicyPackVersions')
          AND name = N'IX_PolicyPackVersions_PolicyPackId_Version')
        DROP INDEX IX_PolicyPackVersions_PolicyPackId_Version ON dbo.PolicyPackVersions;

    CREATE UNIQUE NONCLUSTERED INDEX UQ_PolicyPackVersions_PolicyPackId_Version
        ON dbo.PolicyPackVersions (PolicyPackId, [Version]);
END;
GO

/* 081: Trial funnel first manifest timestamp (see Migrations/081_Tenants_TrialFirstManifestCommittedUtc.sql). */
IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Tenants', N'TrialFirstManifestCommittedUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD TrialFirstManifestCommittedUtc DATETIMEOFFSET NULL;
END;
GO

/* 082: Tenant customer notification channel toggles (see Migrations/082_TenantNotificationChannelPreferences.sql). */
IF OBJECT_ID(N'dbo.TenantNotificationChannelPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantNotificationChannelPreferences
    (
        TenantId                                UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_TenantNotificationChannelPreferences2 PRIMARY KEY,
        SchemaVersion                           INT              NOT NULL
            CONSTRAINT DF_TenantNotificationChannelPreferences_SchemaVersion2 DEFAULT 1,
        EmailCustomerNotificationsEnabled       BIT              NOT NULL
            CONSTRAINT DF_TenantNotificationChannelPreferences_Email2 DEFAULT 1,
        TeamsCustomerNotificationsEnabled       BIT              NOT NULL
            CONSTRAINT DF_TenantNotificationChannelPreferences_Teams2 DEFAULT 0,
        OutboundWebhookCustomerNotificationsEnabled BIT          NOT NULL
            CONSTRAINT DF_TenantNotificationChannelPreferences_Webhook2 DEFAULT 0,
        UpdatedUtc                              DATETIME2(7)     NOT NULL
            CONSTRAINT DF_TenantNotificationChannelPreferences_UpdatedUtc2 DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_TenantNotificationChannelPreferences2_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );
END;
GO

/* 103: Weekly executive digest email preferences (see Migrations/103_TenantExecDigestPreferences.sql). */
IF OBJECT_ID(N'dbo.TenantExecDigestPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantExecDigestPreferences
    (
        TenantId                    UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_TenantExecDigestPreferences2 PRIMARY KEY,
        SchemaVersion               INT              NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_SchemaVersion2 DEFAULT 1,
        EmailEnabled                BIT              NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_EmailEnabled2 DEFAULT 0,
        RecipientEmails             NVARCHAR(2000) NULL,
        IanaTimeZoneId              NVARCHAR(128)  NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_Tz2 DEFAULT N'UTC',
        DayOfWeek                   TINYINT          NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_Dow2 DEFAULT 1,
        HourOfDay                   TINYINT          NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_Hour2 DEFAULT 8,
        UpdatedUtc                  DATETIME2(7)     NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_UpdatedUtc2 DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_TenantExecDigestPreferences_Dow2 CHECK (DayOfWeek BETWEEN 0 AND 6),
        CONSTRAINT CK_TenantExecDigestPreferences_Hour2 CHECK (HourOfDay BETWEEN 0 AND 23),
        CONSTRAINT FK_TenantExecDigestPreferences_Tenants2 FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );
END;
GO

/* 083: Tenant health scores + product feedback (see Migrations/083_TenantHealthScores_ProductFeedback.sql). */
IF OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantHealthScores
    (
        TenantId          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TenantHealthScores2 PRIMARY KEY,
        WorkspaceId       UNIQUEIDENTIFIER NOT NULL,
        ProjectId         UNIQUEIDENTIFIER NOT NULL,
        EngagementScore   DECIMAL(5, 2)    NOT NULL,
        BreadthScore      DECIMAL(5, 2)    NOT NULL,
        QualityScore      DECIMAL(5, 2)    NOT NULL,
        GovernanceScore   DECIMAL(5, 2)    NOT NULL,
        SupportScore      DECIMAL(5, 2)    NOT NULL,
        CompositeScore    DECIMAL(5, 2)    NOT NULL,
        UpdatedUtc        DATETIME2(7)     NOT NULL CONSTRAINT DF_TenantHealthScores_UpdatedUtc2 DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_TenantHealthScores_Tenants2 FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductFeedback', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductFeedback
    (
        FeedbackId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductFeedback2 PRIMARY KEY,
        TenantId     UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId  UNIQUEIDENTIFIER NOT NULL,
        ProjectId    UNIQUEIDENTIFIER NOT NULL,
        FindingRef   NVARCHAR(512)    NULL,
        RunId        UNIQUEIDENTIFIER NULL,
        Score        SMALLINT         NOT NULL,
        CommentText  NVARCHAR(2000)   NULL,
        CreatedUtc   DATETIME2(7)     NOT NULL CONSTRAINT DF_ProductFeedback_CreatedUtc2 DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_ProductFeedback_Score2 CHECK (Score BETWEEN (-1) AND 1),
        CONSTRAINT FK_ProductFeedback_Tenants2 FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );

    CREATE NONCLUSTERED INDEX IX_ProductFeedback_Tenant_CreatedUtc2
        ON dbo.ProductFeedback (TenantId, CreatedUtc DESC);
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'TenantHealthScores')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.ProductFeedback', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'ProductFeedback')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback BEFORE DELETE;
END;
GO

IF OBJECT_ID(N'dbo.sp_TenantHealthScores_Upsert', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_TenantHealthScores_Upsert;
GO

CREATE PROCEDURE dbo.sp_TenantHealthScores_Upsert
    @TenantId uniqueidentifier,
    @WorkspaceId uniqueidentifier,
    @ProjectId uniqueidentifier,
    @EngagementScore decimal(5, 2),
    @BreadthScore decimal(5, 2),
    @QualityScore decimal(5, 2),
    @GovernanceScore decimal(5, 2),
    @SupportScore decimal(5, 2),
    @CompositeScore decimal(5, 2)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.TenantHealthScores AS t
    USING (SELECT @TenantId AS TenantId) AS s ON t.TenantId = s.TenantId
    WHEN MATCHED THEN
        UPDATE SET
            WorkspaceId = @WorkspaceId,
            ProjectId = @ProjectId,
            EngagementScore = @EngagementScore,
            BreadthScore = @BreadthScore,
            QualityScore = @QualityScore,
            GovernanceScore = @GovernanceScore,
            SupportScore = @SupportScore,
            CompositeScore = @CompositeScore,
            UpdatedUtc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (
            TenantId, WorkspaceId, ProjectId,
            EngagementScore, BreadthScore, QualityScore, GovernanceScore, SupportScore,
            CompositeScore, UpdatedUtc)
        VALUES (
            @TenantId, @WorkspaceId, @ProjectId,
            @EngagementScore, @BreadthScore, @QualityScore, @GovernanceScore, @SupportScore,
            @CompositeScore, SYSUTCDATETIME());
END;
GO

IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
   AND OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.TenantHealthScores')
          AND dp.permission_name = N'INSERT'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
    BEGIN
        DENY INSERT ON dbo.TenantHealthScores TO [ArchLucidApp];
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.TenantHealthScores')
          AND dp.permission_name = N'UPDATE'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
    BEGIN
        DENY UPDATE ON dbo.TenantHealthScores TO [ArchLucidApp];
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.TenantHealthScores')
          AND dp.permission_name = N'DELETE'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
    BEGIN
        DENY DELETE ON dbo.TenantHealthScores TO [ArchLucidApp];
    END;

    GRANT EXECUTE ON OBJECT::dbo.sp_TenantHealthScores_Upsert TO [ArchLucidApp];
END;
GO

/* 084: PAGE rowstore compression on AuditEvents + AgentExecutionTraces (see Migrations/084_PageCompression_AuditEvents_AgentExecutionTraces.sql). */
IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.AuditEvents')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.AuditEvents REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

IF OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.AgentExecutionTraces')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.AgentExecutionTraces REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

/* 085: PAGE rowstore compression on dbo.Runs (see Migrations/085_PageCompression_Runs.sql). */
IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.Runs')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.Runs REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

/* 087: PAGE rowstore compression on dbo.DecisionTraces (see Migrations/087_PageCompression_DecisionTraces.sql). */
IF OBJECT_ID(N'dbo.DecisionTraces', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.DecisionTraces')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.DecisionTraces REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

/* 088: PAGE rowstore compression on dbo.DecisioningTraces (see Migrations/088_PageCompression_DecisioningTraces.sql). */
IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.DecisioningTraces')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.DecisioningTraces REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

/* 089: PAGE rowstore compression on dbo.UsageEvents (see Migrations/089_PageCompression_UsageEvents.sql). */
IF OBJECT_ID(N'dbo.UsageEvents', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.UsageEvents')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.UsageEvents REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

/* 090: PAGE rowstore compression on dbo.AlertRecords + dbo.AlertDeliveryAttempts (see Migrations/090_PageCompression_AlertRecords_AlertDeliveryAttempts.sql). */
IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.AlertRecords')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.AlertRecords REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

IF OBJECT_ID(N'dbo.AlertDeliveryAttempts', N'U') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.partitions AS p
            ON p.object_id = i.object_id AND p.index_id = i.index_id
        WHERE i.object_id = OBJECT_ID(N'dbo.AlertDeliveryAttempts')
          AND i.is_disabled = 0
          AND i.type IN (0, 1, 2)
          AND p.data_compression_desc <> N'PAGE')
BEGIN
    ALTER INDEX ALL ON dbo.AlertDeliveryAttempts REBUILD WITH (DATA_COMPRESSION = PAGE, SORT_IN_TEMPDB = ON);
END;
GO

/* 091: RCSI is applied after DbUp by DatabaseMigrator (ALTER DATABASE cannot run inside typical batch transactions). */
SELECT 1;
GO

/* 092: FK outbox + alerts batch 1 (see Migrations/092_FK_Outbox_Alerts_Batch1.sql). */
IF OBJECT_ID(N'dbo.IntegrationEventOutbox', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    UPDATE o
    SET RunId = NULL
    FROM dbo.IntegrationEventOutbox AS o
    WHERE o.RunId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = o.RunId);
END;
GO

IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    UPDATE ar
    SET RunId = NULL
    FROM dbo.AlertRecords AS ar
    WHERE ar.RunId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = ar.RunId);

    UPDATE ar
    SET ComparedToRunId = NULL
    FROM dbo.AlertRecords AS ar
    WHERE ar.ComparedToRunId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = ar.ComparedToRunId);
END;
GO

IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
BEGIN
    UPDATE ar
    SET RecommendationId = NULL
    FROM dbo.AlertRecords AS ar
    WHERE ar.RecommendationId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.RecommendationRecords AS rr WHERE rr.RecommendationId = ar.RecommendationId);
END;
GO

IF OBJECT_ID(N'dbo.AlertDeliveryAttempts', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.AlertRoutingSubscriptions', N'U') IS NOT NULL
BEGIN
    DELETE ada
    FROM dbo.AlertDeliveryAttempts AS ada
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AlertRecords AS ar WHERE ar.AlertId = ada.AlertId)
       OR NOT EXISTS (SELECT 1 FROM dbo.AlertRoutingSubscriptions AS rs WHERE rs.RoutingSubscriptionId = ada.RoutingSubscriptionId);
END;
GO

IF OBJECT_ID(N'dbo.IntegrationEventOutbox', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_IntegrationEventOutbox_Runs_RunId')
BEGIN
    ALTER TABLE dbo.IntegrationEventOutbox ADD CONSTRAINT FK_IntegrationEventOutbox_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.RetrievalIndexingOutbox', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RetrievalIndexingOutbox_Runs_RunId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.RetrievalIndexingOutbox AS o
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = o.RunId))
BEGIN
    ALTER TABLE dbo.RetrievalIndexingOutbox ADD CONSTRAINT FK_RetrievalIndexingOutbox_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.AuthorityPipelineWorkOutbox', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AuthorityPipelineWorkOutbox_Runs_RunId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.AuthorityPipelineWorkOutbox AS o
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = o.RunId))
BEGIN
    ALTER TABLE dbo.AuthorityPipelineWorkOutbox ADD CONSTRAINT FK_AuthorityPipelineWorkOutbox_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.AlertRules', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AlertRecords_AlertRules_RuleId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.AlertRecords AS ar
        WHERE NOT EXISTS (SELECT 1 FROM dbo.AlertRules AS ru WHERE ru.RuleId = ar.RuleId))
BEGIN
    ALTER TABLE dbo.AlertRecords ADD CONSTRAINT FK_AlertRecords_AlertRules_RuleId
        FOREIGN KEY (RuleId) REFERENCES dbo.AlertRules (RuleId);
END;
GO

IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AlertRecords_Runs_RunId')
BEGIN
    ALTER TABLE dbo.AlertRecords ADD CONSTRAINT FK_AlertRecords_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AlertRecords_Runs_ComparedToRunId')
BEGIN
    ALTER TABLE dbo.AlertRecords ADD CONSTRAINT FK_AlertRecords_Runs_ComparedToRunId
        FOREIGN KEY (ComparedToRunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AlertRecords_RecommendationRecords_RecommendationId')
BEGIN
    ALTER TABLE dbo.AlertRecords ADD CONSTRAINT FK_AlertRecords_RecommendationRecords_RecommendationId
        FOREIGN KEY (RecommendationId) REFERENCES dbo.RecommendationRecords (RecommendationId);
END;
GO

IF OBJECT_ID(N'dbo.AlertDeliveryAttempts', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AlertDeliveryAttempts_AlertRecords_AlertId')
BEGIN
    ALTER TABLE dbo.AlertDeliveryAttempts ADD CONSTRAINT FK_AlertDeliveryAttempts_AlertRecords_AlertId
        FOREIGN KEY (AlertId) REFERENCES dbo.AlertRecords (AlertId);
END;
GO

IF OBJECT_ID(N'dbo.AlertDeliveryAttempts', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.AlertRoutingSubscriptions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AlertDeliveryAttempts_AlertRoutingSubscriptions_RoutingSubscriptionId')
BEGIN
    ALTER TABLE dbo.AlertDeliveryAttempts ADD CONSTRAINT FK_AlertDeliveryAttempts_AlertRoutingSubscriptions_RoutingSubscriptionId
        FOREIGN KEY (RoutingSubscriptionId) REFERENCES dbo.AlertRoutingSubscriptions (RoutingSubscriptionId);
END;
GO

/* 093: FK audit + recommendations + conversation messages batch 2 (see Migrations/093_FK_Audit_Recommendations_ConversationMessages_Batch2.sql). */
IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    UPDATE ae
    SET RunId = NULL
    FROM dbo.AuditEvents AS ae
    WHERE ae.RunId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = ae.RunId);
END;
GO

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
BEGIN
    UPDATE ae
    SET ManifestId = NULL
    FROM dbo.AuditEvents AS ae
    WHERE ae.ManifestId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.GoldenManifests AS gm WHERE gm.ManifestId = ae.ManifestId);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    UPDATE rr
    SET ComparedToRunId = NULL
    FROM dbo.RecommendationRecords AS rr
    WHERE rr.ComparedToRunId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = rr.ComparedToRunId);
END;
GO

IF OBJECT_ID(N'dbo.ConversationMessages', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ConversationThreads', N'U') IS NOT NULL
BEGIN
    DELETE cm
    FROM dbo.ConversationMessages AS cm
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ConversationThreads AS ct WHERE ct.ThreadId = cm.ThreadId);
END;
GO

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AuditEvents_Runs_RunId')
BEGIN
    ALTER TABLE dbo.AuditEvents ADD CONSTRAINT FK_AuditEvents_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AuditEvents_GoldenManifests_ManifestId')
BEGIN
    ALTER TABLE dbo.AuditEvents ADD CONSTRAINT FK_AuditEvents_GoldenManifests_ManifestId
        FOREIGN KEY (ManifestId) REFERENCES dbo.GoldenManifests (ManifestId);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecommendationRecords_Runs_RunId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.RecommendationRecords AS rr
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = rr.RunId))
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT FK_RecommendationRecords_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecommendationRecords_Runs_ComparedToRunId')
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT FK_RecommendationRecords_Runs_ComparedToRunId
        FOREIGN KEY (ComparedToRunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.ConversationMessages', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ConversationThreads', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConversationMessages_ConversationThreads_ThreadId')
BEGIN
    ALTER TABLE dbo.ConversationMessages ADD CONSTRAINT FK_ConversationMessages_ConversationThreads_ThreadId
        FOREIGN KEY (ThreadId) REFERENCES dbo.ConversationThreads (ThreadId);
END;
GO

/* 094: RowVersionStamp on AlertRecords, RecommendationRecords, BackgroundJobs (see Migrations/094_RowVersion_AlertRecords_RecommendationRecords_BackgroundJobs.sql). */
IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AlertRecords', N'RowVersionStamp') IS NULL
    ALTER TABLE dbo.AlertRecords ADD RowVersionStamp ROWVERSION;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.RecommendationRecords', N'RowVersionStamp') IS NULL
    ALTER TABLE dbo.RecommendationRecords ADD RowVersionStamp ROWVERSION;
GO

IF OBJECT_ID(N'dbo.BackgroundJobs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.BackgroundJobs', N'RowVersionStamp') IS NULL
    ALTER TABLE dbo.BackgroundJobs ADD RowVersionStamp ROWVERSION;
GO

/* 095: CHECK status/severity/urgency domains (see Migrations/095_CheckConstraints_StatusDomains_Batch.sql). */
IF OBJECT_ID(N'dbo.PolicyPacks', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PolicyPacks_Status')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.PolicyPacks AS p
        WHERE p.Status NOT IN (N'Draft', N'Active', N'Retired'))
BEGIN
    ALTER TABLE dbo.PolicyPacks ADD CONSTRAINT CK_PolicyPacks_Status
        CHECK (Status IN (N'Draft', N'Active', N'Retired'));
END;
GO

IF OBJECT_ID(N'dbo.AlertDeliveryAttempts', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AlertDeliveryAttempts_Status')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.AlertDeliveryAttempts AS a
        WHERE a.Status NOT IN (N'Started', N'Succeeded', N'Failed'))
BEGIN
    ALTER TABLE dbo.AlertDeliveryAttempts ADD CONSTRAINT CK_AlertDeliveryAttempts_Status
        CHECK (Status IN (N'Started', N'Succeeded', N'Failed'));
END;
GO

IF OBJECT_ID(N'dbo.AlertRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AlertRecords_Severity')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.AlertRecords AS ar
        WHERE ar.Severity NOT IN (N'Info', N'Warning', N'High', N'Critical'))
BEGIN
    ALTER TABLE dbo.AlertRecords ADD CONSTRAINT CK_AlertRecords_Severity
        CHECK (Severity IN (N'Info', N'Warning', N'High', N'Critical'));
END;
GO

IF OBJECT_ID(N'dbo.AlertRules', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AlertRules_Severity')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.AlertRules AS r
        WHERE r.Severity NOT IN (N'Info', N'Warning', N'High', N'Critical'))
BEGIN
    ALTER TABLE dbo.AlertRules ADD CONSTRAINT CK_AlertRules_Severity
        CHECK (Severity IN (N'Info', N'Warning', N'High', N'Critical'));
END;
GO

IF OBJECT_ID(N'dbo.AlertRoutingSubscriptions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AlertRoutingSubscriptions_MinimumSeverity')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.AlertRoutingSubscriptions AS s
        WHERE s.MinimumSeverity NOT IN (N'Info', N'Warning', N'High', N'Critical'))
BEGIN
    ALTER TABLE dbo.AlertRoutingSubscriptions ADD CONSTRAINT CK_AlertRoutingSubscriptions_MinimumSeverity
        CHECK (MinimumSeverity IN (N'Info', N'Warning', N'High', N'Critical'));
END;
GO

IF OBJECT_ID(N'dbo.CompositeAlertRules', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CompositeAlertRules_Severity')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.CompositeAlertRules AS c
        WHERE c.Severity NOT IN (N'Info', N'Warning', N'High', N'Critical'))
BEGIN
    ALTER TABLE dbo.CompositeAlertRules ADD CONSTRAINT CK_CompositeAlertRules_Severity
        CHECK (Severity IN (N'Info', N'Warning', N'High', N'Critical'));
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_Urgency')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.RecommendationRecords AS rr
        WHERE rr.Urgency NOT IN (N'Critical', N'High', N'Medium', N'Low'))
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT CK_RecommendationRecords_Urgency
        CHECK (Urgency IN (N'Critical', N'High', N'Medium', N'Low'));
END;
GO

/* 096: ISJSON checks on core payload columns (see Migrations/096_CheckJson_CorePayloadColumns.sql). */
IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AuditEvents_DataJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.AuditEvents AS t WHERE ISJSON(t.DataJson) <> 1)
BEGIN
    ALTER TABLE dbo.AuditEvents ADD CONSTRAINT CK_AuditEvents_DataJson_IsJson
        CHECK (ISJSON(DataJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AgentExecutionTraces_TraceJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.AgentExecutionTraces AS t WHERE ISJSON(t.TraceJson) <> 1)
BEGIN
    ALTER TABLE dbo.AgentExecutionTraces ADD CONSTRAINT CK_AgentExecutionTraces_TraceJson_IsJson
        CHECK (ISJSON(TraceJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.AgentResults', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AgentResults_ResultJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.AgentResults AS t WHERE ISJSON(t.ResultJson) <> 1)
BEGIN
    ALTER TABLE dbo.AgentResults ADD CONSTRAINT CK_AgentResults_ResultJson_IsJson
        CHECK (ISJSON(ResultJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.ComparisonRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ComparisonRecords_PayloadJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.ComparisonRecords AS t WHERE ISJSON(t.PayloadJson) <> 1)
BEGIN
    ALTER TABLE dbo.ComparisonRecords ADD CONSTRAINT CK_ComparisonRecords_PayloadJson_IsJson
        CHECK (ISJSON(PayloadJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.DecisionTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisionTraces_EventJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.DecisionTraces AS t WHERE ISJSON(t.EventJson) <> 1)
BEGIN
    ALTER TABLE dbo.DecisionTraces ADD CONSTRAINT CK_DecisionTraces_EventJson_IsJson
        CHECK (ISJSON(EventJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_AppliedRuleIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.DecisioningTraces AS t WHERE ISJSON(t.AppliedRuleIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT CK_DecisioningTraces_AppliedRuleIdsJson_IsJson
        CHECK (ISJSON(AppliedRuleIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_AcceptedFindingIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.DecisioningTraces AS t WHERE ISJSON(t.AcceptedFindingIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT CK_DecisioningTraces_AcceptedFindingIdsJson_IsJson
        CHECK (ISJSON(AcceptedFindingIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_RejectedFindingIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.DecisioningTraces AS t WHERE ISJSON(t.RejectedFindingIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT CK_DecisioningTraces_RejectedFindingIdsJson_IsJson
        CHECK (ISJSON(RejectedFindingIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DecisioningTraces_NotesJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.DecisioningTraces AS t WHERE ISJSON(t.NotesJson) <> 1)
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT CK_DecisioningTraces_NotesJson_IsJson
        CHECK (ISJSON(NotesJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.AuthorityPipelineWorkOutbox', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AuthorityPipelineWorkOutbox_PayloadJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.AuthorityPipelineWorkOutbox AS t WHERE ISJSON(t.PayloadJson) <> 1)
BEGIN
    ALTER TABLE dbo.AuthorityPipelineWorkOutbox ADD CONSTRAINT CK_AuthorityPipelineWorkOutbox_PayloadJson_IsJson
        CHECK (ISJSON(PayloadJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_SupportingFindingIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.RecommendationRecords AS t WHERE ISJSON(t.SupportingFindingIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT CK_RecommendationRecords_SupportingFindingIdsJson_IsJson
        CHECK (ISJSON(SupportingFindingIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_SupportingDecisionIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.RecommendationRecords AS t WHERE ISJSON(t.SupportingDecisionIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT CK_RecommendationRecords_SupportingDecisionIdsJson_IsJson
        CHECK (ISJSON(SupportingDecisionIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.RecommendationRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RecommendationRecords_SupportingArtifactIdsJson_IsJson')
   AND NOT EXISTS (SELECT 1 FROM dbo.RecommendationRecords AS t WHERE ISJSON(t.SupportingArtifactIdsJson) <> 1)
BEGIN
    ALTER TABLE dbo.RecommendationRecords ADD CONSTRAINT CK_RecommendationRecords_SupportingArtifactIdsJson_IsJson
        CHECK (ISJSON(SupportingArtifactIdsJson) = 1);
END;
GO

IF OBJECT_ID(N'dbo.BackgroundJobs', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.check_constraints
       WHERE name = N'CK_BackgroundJobs_WorkUnitJson_IsJson'
         AND parent_object_id = OBJECT_ID(N'dbo.BackgroundJobs'))
BEGIN
    EXEC (N'
        IF NOT EXISTS (SELECT 1 FROM dbo.BackgroundJobs AS t WHERE ISJSON(t.WorkUnitJson) <> 1)
            ALTER TABLE dbo.BackgroundJobs ADD CONSTRAINT CK_BackgroundJobs_WorkUnitJson_IsJson
                CHECK (ISJSON(WorkUnitJson) = 1);
    ');
END;
GO

/* 096: RLS tenant-only predicate + SentEmails / TenantLifecycleTransitions / TenantTrialSeatOccupants (see Migrations/096_RlsTenantIdOnlyTables.sql). */
IF OBJECT_ID(N'rls.archiforge_tenant_predicate', N'IF') IS NOT NULL
    DROP FUNCTION rls.archiforge_tenant_predicate;
GO

CREATE FUNCTION rls.archiforge_tenant_predicate(@TenantId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS access_granted
    WHERE ISNULL(TRY_CONVERT(int, SESSION_CONTEXT(N'af_rls_bypass')), 0) = 1
       OR @TenantId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'af_tenant_id'))
);
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.SentEmails', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'SentEmails')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.SentEmails,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.SentEmails AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.SentEmails AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.SentEmails BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.TenantLifecycleTransitions', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'TenantLifecycleTransitions')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.TenantTrialSeatOccupants', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'TenantTrialSeatOccupants')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants BEFORE DELETE;
END;
GO

/* 097: TenantOnboardingState + RLS (see Migrations/097_TenantOnboardingState.sql). */
IF OBJECT_ID(N'dbo.TenantOnboardingState', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantOnboardingState
    (
        TenantId                 UNIQUEIDENTIFIER NOT NULL,
        FirstSessionCompletedUtc DATETIME2(7)     NULL,
        CONSTRAINT PK_TenantOnboardingState PRIMARY KEY CLUSTERED (TenantId),
        CONSTRAINT FK_TenantOnboardingState_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.TenantOnboardingState', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archiforge_tenant_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'TenantOnboardingState')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantOnboardingState,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantOnboardingState AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantOnboardingState AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantOnboardingState BEFORE DELETE;
END;
GO

/* 098: IntegrationEventOutbox dead-letter + pending-with-retry indexes (see Migrations/098_OutboxDeadLetterStuckRowIndexes.sql). */
IF OBJECT_ID(N'dbo.IntegrationEventOutbox', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.IntegrationEventOutbox', N'DeadLetteredUtc') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_IntegrationEventOutbox_DeadLetteredUtc'
          AND object_id = OBJECT_ID(N'dbo.IntegrationEventOutbox'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_IntegrationEventOutbox_DeadLetteredUtc
        ON dbo.IntegrationEventOutbox (DeadLetteredUtc DESC, EventType)
        INCLUDE (TenantId, WorkspaceId, ProjectId, RetryCount, LastErrorMessage)
        WHERE DeadLetteredUtc IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.IntegrationEventOutbox', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.IntegrationEventOutbox', N'RetryCount') IS NOT NULL
   AND COL_LENGTH(N'dbo.IntegrationEventOutbox', N'NextRetryUtc') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_IntegrationEventOutbox_PendingWithRetries'
          AND object_id = OBJECT_ID(N'dbo.IntegrationEventOutbox'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_IntegrationEventOutbox_PendingWithRetries
        ON dbo.IntegrationEventOutbox (NextRetryUtc ASC, CreatedUtc ASC)
        INCLUDE (EventType, TenantId, WorkspaceId, ProjectId, RetryCount, LastErrorMessage)
        WHERE ProcessedUtc IS NULL AND DeadLetteredUtc IS NULL AND RetryCount > 0;
END;
GO

/* 099: Data consistency quarantine (see Migrations/099_DataConsistencyQuarantine.sql). */
IF OBJECT_ID(N'dbo.DataConsistencyQuarantine', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DataConsistencyQuarantine
    (
        QuarantineId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_DataConsistencyQuarantine PRIMARY KEY CLUSTERED,
        TenantId     UNIQUEIDENTIFIER NOT NULL,
        SourceTable  NVARCHAR(128)     NOT NULL,
        SourceColumn NVARCHAR(128)     NOT NULL,
        SourceRowKey NVARCHAR(256)     NOT NULL,
        DetectedUtc  DATETIME2(7)      NOT NULL,
        ReasonJson   NVARCHAR(MAX)     NULL,
        CONSTRAINT UQ_DataConsistencyQuarantine_Source UNIQUE (SourceTable, SourceColumn, SourceRowKey)
    );

    CREATE NONCLUSTERED INDEX IX_DataConsistencyQuarantine_TenantId_DetectedUtc
        ON dbo.DataConsistencyQuarantine (TenantId, DetectedUtc DESC);
END;
GO

/* 102: Confluence Cloud publisher targets + jobs (see Migrations/102_ConfluencePublishing.sql). */
IF OBJECT_ID(N'dbo.ConfluencePublishingTargets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfluencePublishingTargets
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ConfluencePublishingTargets PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        BaseUrl NVARCHAR(512) NOT NULL,
        SpaceKey NVARCHAR(64) NOT NULL,
        ParentPageId NVARCHAR(64) NULL,
        AuthorEmail NVARCHAR(320) NOT NULL,
        SecretReference NVARCHAR(256) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ConfluencePublishingTargets_IsActive DEFAULT (1),
        CreatedUtc DATETIMEOFFSET(7) NOT NULL,
        CreatedBy NVARCHAR(320) NOT NULL,
        UpdatedUtc DATETIMEOFFSET(7) NULL
    );

    CREATE UNIQUE INDEX UX_ConfluencePublishingTargets_TenantProject
        ON dbo.ConfluencePublishingTargets (TenantId, WorkspaceId, ProjectId)
        WHERE IsActive = 1;
END;
GO

IF OBJECT_ID(N'dbo.ConfluencePublishJobs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfluencePublishJobs
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ConfluencePublishJobs PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        TargetId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestVersion NVARCHAR(64) NOT NULL,
        DiffBadgeState NVARCHAR(16) NOT NULL,
        PreviousBadgeState NVARCHAR(16) NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        IdempotencyKey VARBINARY(32) NOT NULL,
        Status NVARCHAR(16) NOT NULL,
        Attempts INT NOT NULL CONSTRAINT DF_ConfluencePublishJobs_Attempts DEFAULT (0),
        NextAttemptUtc DATETIMEOFFSET(7) NOT NULL,
        LastErrorReason NVARCHAR(64) NULL,
        LastErrorMessage NVARCHAR(2000) NULL,
        ConfluencePageId NVARCHAR(64) NULL,
        EnqueuedUtc DATETIMEOFFSET(7) NOT NULL,
        CompletedUtc DATETIMEOFFSET(7) NULL,
        CONSTRAINT FK_ConfluencePublishJobs_ConfluencePublishingTargets FOREIGN KEY (TargetId)
            REFERENCES dbo.ConfluencePublishingTargets (Id),
        CONSTRAINT UX_ConfluencePublishJobs_IdempotencyKey UNIQUE (TenantId, IdempotencyKey)
    );

    CREATE INDEX IX_ConfluencePublishJobs_NextAttempt
        ON dbo.ConfluencePublishJobs (Status, NextAttemptUtc)
        INCLUDE (TenantId, TargetId);
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.ConfluencePublishingTargets', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archiforge_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'ConfluencePublishingTargets')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.ConfluencePublishJobs', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archiforge_scope_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'ConfluencePublishJobs')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs BEFORE DELETE;
END;
GO
