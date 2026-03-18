/*
  ArchiForge — SQL Server consolidated schema (reference + greenfield deploy)

  Source of truth: DbUp migrations under ArchiForge.Data/Migrations/
    001_InitialSchema.sql
    002_ComparisonRecords.sql
    003_ComparisonRecords_LabelAndTags.sql
    004_DecisionNodes_And_Evaluations.sql
    005_ArchitectureRuns_ContextSnapshotId.sql
    006_ArchitectureRuns_GraphSnapshotId.sql

  Relations: FOREIGN KEY constraints document and enforce parent/child links.
  Deletion: NO ACTION (no CASCADE) — application owns lifecycle.

  Indexes: tuned for common access paths (run-scoped queries, comparison search,
  export history). Adjust after measuring production workloads.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---- Core request / run ---- */

CREATE TABLE ArchitectureRequests
(
    RequestId NVARCHAR(64) NOT NULL PRIMARY KEY,
    SystemName NVARCHAR(200) NOT NULL,
    Environment NVARCHAR(50) NOT NULL,
    CloudProvider NVARCHAR(50) NOT NULL,
    RequestJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE TABLE ArchitectureRuns
(
    RunId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RequestId NVARCHAR(64) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CompletedUtc DATETIME2 NULL,
    CurrentManifestVersion NVARCHAR(50) NULL,
    ContextSnapshotId NVARCHAR(64) NULL,
    GraphSnapshotId UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_ArchitectureRuns_Request
        FOREIGN KEY (RequestId) REFERENCES ArchitectureRequests (RequestId)
);

CREATE INDEX IX_ArchitectureRuns_RequestId ON ArchitectureRuns (RequestId);
CREATE INDEX IX_ArchitectureRuns_CreatedUtc ON ArchitectureRuns (CreatedUtc DESC);
CREATE INDEX IX_ArchitectureRuns_ContextSnapshotId ON ArchitectureRuns (ContextSnapshotId)
    WHERE ContextSnapshotId IS NOT NULL;
CREATE INDEX IX_ArchitectureRuns_GraphSnapshotId ON ArchitectureRuns (GraphSnapshotId)
    WHERE GraphSnapshotId IS NOT NULL;

/* ---- Agents ---- */

CREATE TABLE AgentTasks
(
    TaskId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    AgentType NVARCHAR(50) NOT NULL,
    Objective NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CompletedUtc DATETIME2 NULL,
    EvidenceBundleRef NVARCHAR(64) NULL,
    CONSTRAINT FK_AgentTasks_Run FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IX_AgentTasks_RunId ON AgentTasks (RunId);
CREATE INDEX IX_AgentTasks_RunId_AgentType ON AgentTasks (RunId, AgentType);

CREATE TABLE AgentResults
(
    ResultId NVARCHAR(64) NOT NULL PRIMARY KEY,
    TaskId NVARCHAR(64) NOT NULL,
    RunId NVARCHAR(64) NOT NULL,
    AgentType NVARCHAR(50) NOT NULL,
    Confidence FLOAT NOT NULL,
    ResultJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_AgentResults_Task FOREIGN KEY (TaskId) REFERENCES AgentTasks (TaskId),
    CONSTRAINT FK_AgentResults_Run FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IX_AgentResults_RunId ON AgentResults (RunId);
CREATE INDEX IX_AgentResults_TaskId ON AgentResults (TaskId);
CREATE INDEX IX_AgentResults_CreatedUtc ON AgentResults (CreatedUtc DESC);

/* ---- Manifest / evidence ---- */

CREATE TABLE GoldenManifestVersions
(
    ManifestVersion NVARCHAR(50) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    SystemName NVARCHAR(200) NOT NULL,
    ManifestJson NVARCHAR(MAX) NOT NULL,
    ParentManifestVersion NVARCHAR(50) NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_GoldenManifestVersions_Run FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId),
    CONSTRAINT FK_GoldenManifestVersions_Parent FOREIGN KEY (ParentManifestVersion)
        REFERENCES GoldenManifestVersions (ManifestVersion)
);

CREATE INDEX IX_GoldenManifestVersions_RunId ON GoldenManifestVersions (RunId);

CREATE TABLE EvidenceBundles
(
    EvidenceBundleId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RequestDescription NVARCHAR(MAX) NOT NULL,
    EvidenceJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

/* AgentTasks.EvidenceBundleRef is optional; not FK-enforced (may not point at EvidenceBundles). */

CREATE TABLE DecisionTraces
(
    TraceId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    EventDescription NVARCHAR(MAX) NOT NULL,
    EventJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_DecisionTraces_Run FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IX_DecisionTraces_RunId ON DecisionTraces (RunId);
CREATE INDEX IX_DecisionTraces_CreatedUtc ON DecisionTraces (CreatedUtc DESC);

CREATE TABLE AgentEvidencePackages
(
    EvidencePackageId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    RequestId NVARCHAR(64) NOT NULL,
    SystemName NVARCHAR(200) NOT NULL,
    Environment NVARCHAR(50) NOT NULL,
    CloudProvider NVARCHAR(50) NOT NULL,
    EvidenceJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_AgentEvidencePackages_Run FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId),
    CONSTRAINT FK_AgentEvidencePackages_Request FOREIGN KEY (RequestId) REFERENCES ArchitectureRequests (RequestId)
);

CREATE INDEX IX_AgentEvidencePackages_RunId ON AgentEvidencePackages (RunId);

CREATE TABLE AgentExecutionTraces
(
    TraceId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    TaskId NVARCHAR(64) NOT NULL,
    AgentType NVARCHAR(50) NOT NULL,
    ParseSucceeded BIT NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    TraceJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_AgentExecutionTraces_Run FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId),
    CONSTRAINT FK_AgentExecutionTraces_Task FOREIGN KEY (TaskId) REFERENCES AgentTasks (TaskId)
);

CREATE INDEX IX_AgentExecutionTraces_RunId ON AgentExecutionTraces (RunId);
CREATE INDEX IX_AgentExecutionTraces_TaskId ON AgentExecutionTraces (TaskId);

/* ---- Exports & comparisons ---- */

CREATE TABLE RunExportRecords
(
    ExportRecordId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    ExportType NVARCHAR(100) NOT NULL,
    Format NVARCHAR(50) NOT NULL,
    FileName NVARCHAR(260) NOT NULL,
    TemplateProfile NVARCHAR(100) NULL,
    TemplateProfileDisplayName NVARCHAR(200) NULL,
    WasAutoSelected BIT NOT NULL,
    ResolutionReason NVARCHAR(MAX) NULL,
    ManifestVersion NVARCHAR(100) NULL,
    Notes NVARCHAR(MAX) NULL,
    AnalysisRequestJson NVARCHAR(MAX) NULL,
    IncludedEvidence BIT NULL,
    IncludedExecutionTraces BIT NULL,
    IncludedManifest BIT NULL,
    IncludedDiagram BIT NULL,
    IncludedSummary BIT NULL,
    IncludedDeterminismCheck BIT NULL,
    DeterminismIterations INT NULL,
    IncludedManifestCompare BIT NULL,
    CompareManifestVersion NVARCHAR(100) NULL,
    IncludedAgentResultCompare BIT NULL,
    CompareRunId NVARCHAR(64) NULL,
    RecordJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_RunExportRecords_Run FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IX_RunExportRecords_RunId ON RunExportRecords (RunId);
CREATE INDEX IX_RunExportRecords_CreatedUtc ON RunExportRecords (CreatedUtc DESC);

CREATE TABLE ComparisonRecords
(
    ComparisonRecordId NVARCHAR(64) NOT NULL PRIMARY KEY,
    ComparisonType NVARCHAR(100) NOT NULL,
    LeftRunId NVARCHAR(64) NULL,
    RightRunId NVARCHAR(64) NULL,
    LeftManifestVersion NVARCHAR(100) NULL,
    RightManifestVersion NVARCHAR(100) NULL,
    LeftExportRecordId NVARCHAR(64) NULL,
    RightExportRecordId NVARCHAR(64) NULL,
    Format NVARCHAR(50) NOT NULL,
    SummaryMarkdown NVARCHAR(MAX) NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    CreatedUtc DATETIME2 NOT NULL,
    Label NVARCHAR(256) NULL,
    Tags NVARCHAR(MAX) NULL,
    CONSTRAINT FK_ComparisonRecords_LeftRun FOREIGN KEY (LeftRunId) REFERENCES ArchitectureRuns (RunId),
    CONSTRAINT FK_ComparisonRecords_RightRun FOREIGN KEY (RightRunId) REFERENCES ArchitectureRuns (RunId)
    /* Export record IDs indexed but not FK: comparisons may reference exports across retention/deletes. */
);

CREATE INDEX IX_ComparisonRecords_LeftRunId ON ComparisonRecords (LeftRunId);
CREATE INDEX IX_ComparisonRecords_RightRunId ON ComparisonRecords (RightRunId);
CREATE INDEX IX_ComparisonRecords_LeftExportRecordId ON ComparisonRecords (LeftExportRecordId);
CREATE INDEX IX_ComparisonRecords_RightExportRecordId ON ComparisonRecords (RightExportRecordId);
CREATE INDEX IX_ComparisonRecords_ComparisonType_CreatedUtc ON ComparisonRecords (ComparisonType, CreatedUtc DESC);
CREATE INDEX IX_ComparisonRecords_Label ON ComparisonRecords (Label) WHERE Label IS NOT NULL;

/* ---- Decision Engine v2 ---- */

CREATE TABLE DecisionNodes
(
    DecisionId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    Topic NVARCHAR(100) NOT NULL,
    SelectedOptionId NVARCHAR(64) NULL,
    Confidence FLOAT NOT NULL,
    Rationale NVARCHAR(MAX) NOT NULL,
    DecisionJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_DecisionNodes_Run FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IX_DecisionNodes_RunId ON DecisionNodes (RunId);
CREATE INDEX IX_DecisionNodes_CreatedUtc ON DecisionNodes (CreatedUtc DESC);

CREATE TABLE AgentEvaluations
(
    EvaluationId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    TargetAgentTaskId NVARCHAR(64) NOT NULL,
    EvaluationType NVARCHAR(50) NOT NULL,
    ConfidenceDelta FLOAT NOT NULL,
    Rationale NVARCHAR(MAX) NOT NULL,
    EvaluationJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_AgentEvaluations_Run FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId),
    CONSTRAINT FK_AgentEvaluations_Task FOREIGN KEY (TargetAgentTaskId) REFERENCES AgentTasks (TaskId)
);

CREATE INDEX IX_AgentEvaluations_RunId ON AgentEvaluations (RunId);
CREATE INDEX IX_AgentEvaluations_TargetAgentTaskId ON AgentEvaluations (TargetAgentTaskId);
