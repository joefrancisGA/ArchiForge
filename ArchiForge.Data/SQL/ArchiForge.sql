/*
  ArchiForge — SQL Server consolidated schema (idempotent)

  Safe to run multiple times: skips existing tables/indexes/FKs; adds missing columns
  when tables already exist from an older baseline.

  DbUp migrations remain the authoritative upgrade path for deployed apps; this script
  is for greenfield / manual / tooling.

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
        CONSTRAINT FK_ArchitectureRuns_Request FOREIGN KEY (RequestId)
            REFERENCES dbo.ArchitectureRequests (RequestId)
    );
END
GO

/* Additive columns if table predates migrations 005/006 */
IF OBJECT_ID(N'dbo.ArchitectureRuns', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ArchitectureRuns') AND name = N'ContextSnapshotId')
    ALTER TABLE dbo.ArchitectureRuns ADD ContextSnapshotId NVARCHAR(64) NULL;
GO

IF OBJECT_ID(N'dbo.ArchitectureRuns', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ArchitectureRuns') AND name = N'GraphSnapshotId')
    ALTER TABLE dbo.ArchitectureRuns ADD GraphSnapshotId UNIQUEIDENTIFIER NULL;
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
            REFERENCES dbo.ArchitectureRuns (RunId)
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
        CONSTRAINT FK_AgentResults_Run FOREIGN KEY (RunId) REFERENCES dbo.ArchitectureRuns (RunId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentResults_Task')
   AND OBJECT_ID(N'dbo.AgentResults', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AgentResults ADD CONSTRAINT FK_AgentResults_Task FOREIGN KEY (TaskId)
        REFERENCES dbo.AgentTasks (TaskId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentResults_Run')
   AND OBJECT_ID(N'dbo.AgentResults', N'U') IS NOT NULL
BEGIN
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
            REFERENCES dbo.GoldenManifestVersions (ManifestVersion)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifestVersions_Run')
   AND OBJECT_ID(N'dbo.GoldenManifestVersions', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.GoldenManifestVersions ADD CONSTRAINT FK_GoldenManifestVersions_Run FOREIGN KEY (RunId)
        REFERENCES dbo.ArchitectureRuns (RunId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifestVersions_Parent')
   AND OBJECT_ID(N'dbo.GoldenManifestVersions', N'U') IS NOT NULL
BEGIN
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
        CONSTRAINT FK_DecisionTraces_Run FOREIGN KEY (RunId) REFERENCES dbo.ArchitectureRuns (RunId)
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
            REFERENCES dbo.ArchitectureRequests (RequestId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvidencePackages_Run')
   AND OBJECT_ID(N'dbo.AgentEvidencePackages', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AgentEvidencePackages ADD CONSTRAINT FK_AgentEvidencePackages_Run FOREIGN KEY (RunId)
        REFERENCES dbo.ArchitectureRuns (RunId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvidencePackages_Request')
   AND OBJECT_ID(N'dbo.AgentEvidencePackages', N'U') IS NOT NULL
BEGIN
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
            REFERENCES dbo.AgentTasks (TaskId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentExecutionTraces_Run')
   AND OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AgentExecutionTraces ADD CONSTRAINT FK_AgentExecutionTraces_Run FOREIGN KEY (RunId)
        REFERENCES dbo.ArchitectureRuns (RunId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentExecutionTraces_Task')
   AND OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
BEGIN
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
        CONSTRAINT FK_RunExportRecords_Run FOREIGN KEY (RunId) REFERENCES dbo.ArchitectureRuns (RunId)
    );
END
GO

IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'AnalysisRequestJson')
    ALTER TABLE dbo.RunExportRecords ADD AnalysisRequestJson NVARCHAR(MAX) NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'IncludedEvidence')
    ALTER TABLE dbo.RunExportRecords ADD IncludedEvidence BIT NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'IncludedExecutionTraces')
    ALTER TABLE dbo.RunExportRecords ADD IncludedExecutionTraces BIT NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'IncludedManifest')
    ALTER TABLE dbo.RunExportRecords ADD IncludedManifest BIT NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'IncludedDiagram')
    ALTER TABLE dbo.RunExportRecords ADD IncludedDiagram BIT NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'IncludedSummary')
    ALTER TABLE dbo.RunExportRecords ADD IncludedSummary BIT NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'IncludedDeterminismCheck')
    ALTER TABLE dbo.RunExportRecords ADD IncludedDeterminismCheck BIT NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'DeterminismIterations')
    ALTER TABLE dbo.RunExportRecords ADD DeterminismIterations INT NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'IncludedManifestCompare')
    ALTER TABLE dbo.RunExportRecords ADD IncludedManifestCompare BIT NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'CompareManifestVersion')
    ALTER TABLE dbo.RunExportRecords ADD CompareManifestVersion NVARCHAR(100) NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'IncludedAgentResultCompare')
    ALTER TABLE dbo.RunExportRecords ADD IncludedAgentResultCompare BIT NULL;
GO
IF OBJECT_ID(N'dbo.RunExportRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RunExportRecords') AND name = N'CompareRunId')
    ALTER TABLE dbo.RunExportRecords ADD CompareRunId NVARCHAR(64) NULL;
GO

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
            REFERENCES dbo.ArchitectureRuns (RunId)
    );
END
GO

IF OBJECT_ID(N'dbo.ComparisonRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ComparisonRecords') AND name = N'Label')
    ALTER TABLE dbo.ComparisonRecords ADD Label NVARCHAR(256) NULL;
GO
IF OBJECT_ID(N'dbo.ComparisonRecords', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ComparisonRecords') AND name = N'Tags')
    ALTER TABLE dbo.ComparisonRecords ADD Tags NVARCHAR(MAX) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ComparisonRecords_LeftRun')
   AND OBJECT_ID(N'dbo.ComparisonRecords', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ComparisonRecords ADD CONSTRAINT FK_ComparisonRecords_LeftRun FOREIGN KEY (LeftRunId)
        REFERENCES dbo.ArchitectureRuns (RunId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ComparisonRecords_RightRun')
   AND OBJECT_ID(N'dbo.ComparisonRecords', N'U') IS NOT NULL
BEGIN
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
        CONSTRAINT FK_DecisionNodes_Run FOREIGN KEY (RunId) REFERENCES dbo.ArchitectureRuns (RunId)
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
            REFERENCES dbo.AgentTasks (TaskId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvaluations_Run')
   AND OBJECT_ID(N'dbo.AgentEvaluations', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AgentEvaluations ADD CONSTRAINT FK_AgentEvaluations_Run FOREIGN KEY (RunId)
        REFERENCES dbo.ArchitectureRuns (RunId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvaluations_Task')
   AND OBJECT_ID(N'dbo.AgentEvaluations', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AgentEvaluations ADD CONSTRAINT FK_AgentEvaluations_Task FOREIGN KEY (TargetAgentTaskId)
        REFERENCES dbo.AgentTasks (TaskId);
END
GO

/* ---- Indexes (idempotent) ---- */

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.ArchitectureRuns') AND i.name = N'IX_ArchitectureRuns_RequestId')
    CREATE NONCLUSTERED INDEX IX_ArchitectureRuns_RequestId ON dbo.ArchitectureRuns (RequestId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.ArchitectureRuns') AND i.name = N'IX_ArchitectureRuns_CreatedUtc')
    CREATE NONCLUSTERED INDEX IX_ArchitectureRuns_CreatedUtc ON dbo.ArchitectureRuns (CreatedUtc DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.ArchitectureRuns') AND i.name = N'IX_ArchitectureRuns_ContextSnapshotId')
    CREATE NONCLUSTERED INDEX IX_ArchitectureRuns_ContextSnapshotId ON dbo.ArchitectureRuns (ContextSnapshotId)
        WHERE ContextSnapshotId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.ArchitectureRuns') AND i.name = N'IX_ArchitectureRuns_GraphSnapshotId')
    CREATE NONCLUSTERED INDEX IX_ArchitectureRuns_GraphSnapshotId ON dbo.ArchitectureRuns (GraphSnapshotId)
        WHERE GraphSnapshotId IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.AgentTasks') AND i.name = N'IX_AgentTasks_RunId')
    CREATE NONCLUSTERED INDEX IX_AgentTasks_RunId ON dbo.AgentTasks (RunId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.AgentTasks') AND i.name = N'IX_AgentTasks_RunId_AgentType')
    CREATE NONCLUSTERED INDEX IX_AgentTasks_RunId_AgentType ON dbo.AgentTasks (RunId, AgentType);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.AgentResults') AND i.name = N'IX_AgentResults_RunId')
    CREATE NONCLUSTERED INDEX IX_AgentResults_RunId ON dbo.AgentResults (RunId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.AgentResults') AND i.name = N'IX_AgentResults_TaskId')
    CREATE NONCLUSTERED INDEX IX_AgentResults_TaskId ON dbo.AgentResults (TaskId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.AgentResults') AND i.name = N'IX_AgentResults_CreatedUtc')
    CREATE NONCLUSTERED INDEX IX_AgentResults_CreatedUtc ON dbo.AgentResults (CreatedUtc DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.GoldenManifestVersions') AND i.name = N'IX_GoldenManifestVersions_RunId')
    CREATE NONCLUSTERED INDEX IX_GoldenManifestVersions_RunId ON dbo.GoldenManifestVersions (RunId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.DecisionTraces') AND i.name = N'IX_DecisionTraces_RunId')
    CREATE NONCLUSTERED INDEX IX_DecisionTraces_RunId ON dbo.DecisionTraces (RunId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.DecisionTraces') AND i.name = N'IX_DecisionTraces_CreatedUtc')
    CREATE NONCLUSTERED INDEX IX_DecisionTraces_CreatedUtc ON dbo.DecisionTraces (CreatedUtc DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.AgentEvidencePackages') AND i.name = N'IX_AgentEvidencePackages_RunId')
    CREATE NONCLUSTERED INDEX IX_AgentEvidencePackages_RunId ON dbo.AgentEvidencePackages (RunId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.AgentExecutionTraces') AND i.name = N'IX_AgentExecutionTraces_RunId')
    CREATE NONCLUSTERED INDEX IX_AgentExecutionTraces_RunId ON dbo.AgentExecutionTraces (RunId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.AgentExecutionTraces') AND i.name = N'IX_AgentExecutionTraces_TaskId')
    CREATE NONCLUSTERED INDEX IX_AgentExecutionTraces_TaskId ON dbo.AgentExecutionTraces (TaskId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.RunExportRecords') AND i.name = N'IX_RunExportRecords_RunId')
    CREATE NONCLUSTERED INDEX IX_RunExportRecords_RunId ON dbo.RunExportRecords (RunId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.RunExportRecords') AND i.name = N'IX_RunExportRecords_CreatedUtc')
    CREATE NONCLUSTERED INDEX IX_RunExportRecords_CreatedUtc ON dbo.RunExportRecords (CreatedUtc DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.ComparisonRecords') AND i.name = N'IX_ComparisonRecords_LeftRunId')
    CREATE NONCLUSTERED INDEX IX_ComparisonRecords_LeftRunId ON dbo.ComparisonRecords (LeftRunId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.ComparisonRecords') AND i.name = N'IX_ComparisonRecords_RightRunId')
    CREATE NONCLUSTERED INDEX IX_ComparisonRecords_RightRunId ON dbo.ComparisonRecords (RightRunId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.ComparisonRecords') AND i.name = N'IX_ComparisonRecords_LeftExportRecordId')
    CREATE NONCLUSTERED INDEX IX_ComparisonRecords_LeftExportRecordId ON dbo.ComparisonRecords (LeftExportRecordId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.ComparisonRecords') AND i.name = N'IX_ComparisonRecords_RightExportRecordId')
    CREATE NONCLUSTERED INDEX IX_ComparisonRecords_RightExportRecordId ON dbo.ComparisonRecords (RightExportRecordId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.ComparisonRecords') AND i.name = N'IX_ComparisonRecords_ComparisonType_CreatedUtc')
    CREATE NONCLUSTERED INDEX IX_ComparisonRecords_ComparisonType_CreatedUtc
        ON dbo.ComparisonRecords (ComparisonType, CreatedUtc DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.ComparisonRecords') AND i.name = N'IX_ComparisonRecords_Label')
    CREATE NONCLUSTERED INDEX IX_ComparisonRecords_Label ON dbo.ComparisonRecords (Label) WHERE Label IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.DecisionNodes') AND i.name = N'IX_DecisionNodes_RunId')
    CREATE NONCLUSTERED INDEX IX_DecisionNodes_RunId ON dbo.DecisionNodes (RunId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.DecisionNodes') AND i.name = N'IX_DecisionNodes_CreatedUtc')
    CREATE NONCLUSTERED INDEX IX_DecisionNodes_CreatedUtc ON dbo.DecisionNodes (CreatedUtc DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.AgentEvaluations') AND i.name = N'IX_AgentEvaluations_RunId')
    CREATE NONCLUSTERED INDEX IX_AgentEvaluations_RunId ON dbo.AgentEvaluations (RunId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id
               WHERE t.object_id = OBJECT_ID(N'dbo.AgentEvaluations') AND i.name = N'IX_AgentEvaluations_TargetAgentTaskId')
    CREATE NONCLUSTERED INDEX IX_AgentEvaluations_TargetAgentTaskId ON dbo.AgentEvaluations (TargetAgentTaskId);
GO
