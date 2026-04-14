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
        INDEX IX_PolicyPackVersions_PolicyPackId_Version NONCLUSTERED (PolicyPackId, [Version])
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
   Other candidate columns (e.g. PolicyPacks.Status, AlertDeliveryAttempts.Status) left unguarded until a later pass.
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

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Runs_Scope_CreatedUtc'
      AND object_id = OBJECT_ID(N'dbo.Runs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Runs_Scope_CreatedUtc
        ON dbo.Runs (TenantId, WorkspaceId, ScopeProjectId, CreatedUtc DESC)
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
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog
    WITH (STATE = OFF);
GO
