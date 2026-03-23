/*
  ArchiForge — SQL Server consolidated schema (idempotent)

  Safe to run multiple times: skips existing tables/indexes/FKs. Nonclustered indexes are
  declared inline on each CREATE TABLE (INDEX … NONCLUSTERED), not as separate CREATE INDEX
  statements. Additive ALTER batches for columns live only in DbUp migrations where needed;
  this consolidated script assumes greenfield CREATE or that migrations have already been
  applied. Optional batches below may ADD CONSTRAINT (FK) when a legacy table exists but is
  missing a foreign key.

  Upgrading existing databases: use DbUp migrations in ArchiForge.Data/Migrations/.

  Includes:
    - API / agent / commit trail (DbUp 001–007 equivalent)
    - Authority-chain + Dapper persistence + Decisioning (recommendations, advisory,
      digests, alerts, composite rules, policy packs) — same DDL as Persistence bootstrap.

  DbUp migrations remain the authoritative upgrade path for deployed apps; this script
  is for greenfield / manual / tooling. Persistence bootstrap executes this file (copy
  under ArchiForge.Persistence output as Scripts/ArchiForge.sql).

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

IF OBJECT_ID(N'dbo.ArchitectureRuns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArchitectureRuns
    (
        RunId                  NVARCHAR(64)  NOT NULL PRIMARY KEY,
        RequestId              NVARCHAR(64)  NOT NULL,
        Status                 NVARCHAR(50)  NOT NULL,
        CreatedUtc             DATETIME2     NOT NULL,
        CompletedUtc           DATETIME2     NULL,
        CurrentManifestVersion NVARCHAR(50)  NULL,
        ContextSnapshotId      NVARCHAR(64)  NULL,
        GraphSnapshotId        UNIQUEIDENTIFIER NULL,
        ArtifactBundleId       UNIQUEIDENTIFIER NULL,
        CONSTRAINT FK_ArchitectureRuns_Request FOREIGN KEY (RequestId)
            REFERENCES dbo.ArchitectureRequests (RequestId),
        INDEX IX_ArchitectureRuns_RequestId NONCLUSTERED (RequestId),
        INDEX IX_ArchitectureRuns_CreatedUtc NONCLUSTERED (CreatedUtc DESC),
        INDEX IX_ArchitectureRuns_ContextSnapshotId NONCLUSTERED (ContextSnapshotId)
            WHERE (ContextSnapshotId IS NOT NULL),
        INDEX IX_ArchitectureRuns_GraphSnapshotId NONCLUSTERED (GraphSnapshotId)
            WHERE (GraphSnapshotId IS NOT NULL)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArchitectureRuns_Request')
   AND OBJECT_ID(N'dbo.ArchitectureRuns', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ArchitectureRuns
        ADD CONSTRAINT FK_ArchitectureRuns_Request FOREIGN KEY (RequestId)
            REFERENCES dbo.ArchitectureRequests (RequestId);
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
        CONSTRAINT FK_AgentTasks_Run FOREIGN KEY (RunId)
            REFERENCES dbo.ArchitectureRuns (RunId),
        INDEX IX_AgentTasks_RunId NONCLUSTERED (RunId),
        INDEX IX_AgentTasks_RunId_AgentType NONCLUSTERED (RunId, AgentType)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentTasks_Run')
   AND OBJECT_ID(N'dbo.AgentTasks', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AgentTasks ADD CONSTRAINT FK_AgentTasks_Run FOREIGN KEY (RunId)
        REFERENCES dbo.ArchitectureRuns (RunId);
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
        CONSTRAINT FK_AgentResults_Run FOREIGN KEY (RunId) REFERENCES dbo.ArchitectureRuns (RunId),
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
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentResults_Run')
        ALTER TABLE dbo.AgentResults ADD CONSTRAINT FK_AgentResults_Run FOREIGN KEY (RunId)
            REFERENCES dbo.ArchitectureRuns (RunId);
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
        CONSTRAINT FK_GoldenManifestVersions_Run FOREIGN KEY (RunId)
            REFERENCES dbo.ArchitectureRuns (RunId),
        CONSTRAINT FK_GoldenManifestVersions_Parent FOREIGN KEY (ParentManifestVersion)
            REFERENCES dbo.GoldenManifestVersions (ManifestVersion),
        INDEX IX_GoldenManifestVersions_RunId NONCLUSTERED (RunId)
    );
END
GO

IF OBJECT_ID(N'dbo.GoldenManifestVersions', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifestVersions_Run')
        ALTER TABLE dbo.GoldenManifestVersions ADD CONSTRAINT FK_GoldenManifestVersions_Run FOREIGN KEY (RunId)
            REFERENCES dbo.ArchitectureRuns (RunId);
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
        CONSTRAINT FK_DecisionTraces_Run FOREIGN KEY (RunId) REFERENCES dbo.ArchitectureRuns (RunId),
        INDEX IX_DecisionTraces_RunId NONCLUSTERED (RunId),
        INDEX IX_DecisionTraces_CreatedUtc NONCLUSTERED (CreatedUtc DESC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DecisionTraces_Run')
   AND OBJECT_ID(N'dbo.DecisionTraces', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.DecisionTraces ADD CONSTRAINT FK_DecisionTraces_Run FOREIGN KEY (RunId)
        REFERENCES dbo.ArchitectureRuns (RunId);
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
        CONSTRAINT FK_AgentEvidencePackages_Run FOREIGN KEY (RunId)
            REFERENCES dbo.ArchitectureRuns (RunId),
        CONSTRAINT FK_AgentEvidencePackages_Request FOREIGN KEY (RequestId)
            REFERENCES dbo.ArchitectureRequests (RequestId),
        INDEX IX_AgentEvidencePackages_RunId NONCLUSTERED (RunId)
    );
END
GO

IF OBJECT_ID(N'dbo.AgentEvidencePackages', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvidencePackages_Run')
        ALTER TABLE dbo.AgentEvidencePackages ADD CONSTRAINT FK_AgentEvidencePackages_Run FOREIGN KEY (RunId)
            REFERENCES dbo.ArchitectureRuns (RunId);
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
        CONSTRAINT FK_AgentExecutionTraces_Run FOREIGN KEY (RunId)
            REFERENCES dbo.ArchitectureRuns (RunId),
        CONSTRAINT FK_AgentExecutionTraces_Task FOREIGN KEY (TaskId)
            REFERENCES dbo.AgentTasks (TaskId),
        INDEX IX_AgentExecutionTraces_RunId NONCLUSTERED (RunId),
        INDEX IX_AgentExecutionTraces_TaskId NONCLUSTERED (TaskId)
    );
END
GO

IF OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentExecutionTraces_Run')
        ALTER TABLE dbo.AgentExecutionTraces ADD CONSTRAINT FK_AgentExecutionTraces_Run FOREIGN KEY (RunId)
            REFERENCES dbo.ArchitectureRuns (RunId);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentExecutionTraces_Task')
        ALTER TABLE dbo.AgentExecutionTraces ADD CONSTRAINT FK_AgentExecutionTraces_Task FOREIGN KEY (TaskId)
            REFERENCES dbo.AgentTasks (TaskId);
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
        CONSTRAINT FK_RunExportRecords_Run FOREIGN KEY (RunId) REFERENCES dbo.ArchitectureRuns (RunId),
        INDEX IX_RunExportRecords_RunId NONCLUSTERED (RunId),
        INDEX IX_RunExportRecords_CreatedUtc NONCLUSTERED (CreatedUtc DESC)
    );
END
GO

/* RunExportRecords: full column set is in CREATE above (matches DbUp 001); no per-column ALTERs. */

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RunExportRecords_Run')
   AND OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.RunExportRecords ADD CONSTRAINT FK_RunExportRecords_Run FOREIGN KEY (RunId)
        REFERENCES dbo.ArchitectureRuns (RunId);
END
GO

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
        CONSTRAINT FK_ComparisonRecords_LeftRun FOREIGN KEY (LeftRunId)
            REFERENCES dbo.ArchitectureRuns (RunId),
        CONSTRAINT FK_ComparisonRecords_RightRun FOREIGN KEY (RightRunId)
            REFERENCES dbo.ArchitectureRuns (RunId),
        INDEX IX_ComparisonRecords_LeftRunId NONCLUSTERED (LeftRunId),
        INDEX IX_ComparisonRecords_RightRunId NONCLUSTERED (RightRunId),
        INDEX IX_ComparisonRecords_LeftExportRecordId NONCLUSTERED (LeftExportRecordId),
        INDEX IX_ComparisonRecords_RightExportRecordId NONCLUSTERED (RightExportRecordId),
        INDEX IX_ComparisonRecords_ComparisonType_CreatedUtc NONCLUSTERED (ComparisonType, CreatedUtc DESC),
        INDEX IX_ComparisonRecords_Label NONCLUSTERED (Label) WHERE (Label IS NOT NULL)
    );
END
GO

IF OBJECT_ID(N'dbo.ComparisonRecords', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ComparisonRecords_LeftRun')
        ALTER TABLE dbo.ComparisonRecords ADD CONSTRAINT FK_ComparisonRecords_LeftRun FOREIGN KEY (LeftRunId)
            REFERENCES dbo.ArchitectureRuns (RunId);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ComparisonRecords_RightRun')
        ALTER TABLE dbo.ComparisonRecords ADD CONSTRAINT FK_ComparisonRecords_RightRun FOREIGN KEY (RightRunId)
            REFERENCES dbo.ArchitectureRuns (RunId);
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
        CONSTRAINT FK_DecisionNodes_Run FOREIGN KEY (RunId) REFERENCES dbo.ArchitectureRuns (RunId),
        INDEX IX_DecisionNodes_RunId NONCLUSTERED (RunId),
        INDEX IX_DecisionNodes_CreatedUtc NONCLUSTERED (CreatedUtc DESC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DecisionNodes_Run')
   AND OBJECT_ID(N'dbo.DecisionNodes', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.DecisionNodes ADD CONSTRAINT FK_DecisionNodes_Run FOREIGN KEY (RunId)
        REFERENCES dbo.ArchitectureRuns (RunId);
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
        CONSTRAINT FK_AgentEvaluations_Run FOREIGN KEY (RunId)
            REFERENCES dbo.ArchitectureRuns (RunId),
        CONSTRAINT FK_AgentEvaluations_Task FOREIGN KEY (TargetAgentTaskId)
            REFERENCES dbo.AgentTasks (TaskId),
        INDEX IX_AgentEvaluations_RunId NONCLUSTERED (RunId),
        INDEX IX_AgentEvaluations_TargetAgentTaskId NONCLUSTERED (TargetAgentTaskId)
    );
END
GO

IF OBJECT_ID(N'dbo.AgentEvaluations', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvaluations_Run')
        ALTER TABLE dbo.AgentEvaluations ADD CONSTRAINT FK_AgentEvaluations_Run FOREIGN KEY (RunId)
            REFERENCES dbo.ArchitectureRuns (RunId);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvaluations_Task')
        ALTER TABLE dbo.AgentEvaluations ADD CONSTRAINT FK_AgentEvaluations_Task FOREIGN KEY (TargetAgentTaskId)
            REFERENCES dbo.AgentTasks (TaskId);
END
GO

/* ---- Authority / Dapper persistence + Decisioning (GUID Runs; not ArchitectureRuns) ---- */
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
        TenantId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Runs_TenantId DEFAULT ('11111111-1111-1111-1111-111111111111'),
        WorkspaceId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Runs_WorkspaceId DEFAULT ('22222222-2222-2222-2222-222222222222'),
        ScopeProjectId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Runs_ScopeProjectId DEFAULT ('33333333-3333-3333-3333-333333333333'),
        INDEX IX_Runs_ProjectId_CreatedUtc NONCLUSTERED (ProjectId, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.ContextSnapshots', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshots
    (
        SnapshotId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ProjectId NVARCHAR(200) NOT NULL,
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

IF OBJECT_ID('dbo.GraphSnapshots', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GraphSnapshots
    (
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ContextSnapshotId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        NodesJson NVARCHAR(MAX) NOT NULL,
        EdgesJson NVARCHAR(MAX) NOT NULL,
        WarningsJson NVARCHAR(MAX) NOT NULL,
        INDEX IX_GraphSnapshots_RunId NONCLUSTERED (RunId),
        INDEX IX_GraphSnapshots_ContextSnapshotId NONCLUSTERED (ContextSnapshotId)
    );
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
        CreatedUtc DATETIME2 NOT NULL,
        FindingsJson NVARCHAR(MAX) NOT NULL,
        INDEX IX_FindingsSnapshots_RunId NONCLUSTERED (RunId),
        INDEX IX_FindingsSnapshots_ContextSnapshotId NONCLUSTERED (ContextSnapshotId),
        INDEX IX_FindingsSnapshots_GraphSnapshotId NONCLUSTERED (GraphSnapshotId)
    );
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
        TenantId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_DecisioningTraces_TenantId_Create DEFAULT ('11111111-1111-1111-1111-111111111111'),
        WorkspaceId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_DecisioningTraces_WorkspaceId_Create DEFAULT ('22222222-2222-2222-2222-222222222222'),
        ProjectId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_DecisioningTraces_ProjectId_Create DEFAULT ('33333333-3333-3333-3333-333333333333'),
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
        TenantId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_GoldenManifests_TenantId DEFAULT ('11111111-1111-1111-1111-111111111111'),
        WorkspaceId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_GoldenManifests_WorkspaceId DEFAULT ('22222222-2222-2222-2222-222222222222'),
        ProjectId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_GoldenManifests_ProjectId DEFAULT ('33333333-3333-3333-3333-333333333333'),
        INDEX IX_GoldenManifests_RunId NONCLUSTERED (RunId)
    );
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
        ArtifactsJson NVARCHAR(MAX) NOT NULL,
        TraceJson NVARCHAR(MAX) NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_ArtifactBundles_TenantId DEFAULT ('11111111-1111-1111-1111-111111111111'),
        WorkspaceId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_ArtifactBundles_WorkspaceId DEFAULT ('22222222-2222-2222-2222-222222222222'),
        ProjectId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_ArtifactBundles_ProjectId DEFAULT ('33333333-3333-3333-3333-333333333333'),
        INDEX IX_ArtifactBundles_RunId NONCLUSTERED (RunId),
        INDEX IX_ArtifactBundles_ManifestId NONCLUSTERED (ManifestId)
    );
END;
GO

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
        INDEX IX_PolicyPackAssignments_Scope_Enabled NONCLUSTERED (TenantId, WorkspaceId, ProjectId, IsEnabled, AssignedUtc DESC),
        INDEX IX_PolicyPackAssignments_ScopeLevel_AssignedUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, ScopeLevel, AssignedUtc DESC)
    );
END;
GO
