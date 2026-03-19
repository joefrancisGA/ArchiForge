/*
  ArchiForge — SQLite consolidated schema (tests / local / reference)

  Idempotency:
    - CREATE TABLE IF NOT EXISTS / CREATE INDEX IF NOT EXISTS → safe to run repeatedly.
    - SQLite cannot ADD COLUMN idempotently in plain SQL; if you open an older DB file that
      predates a column (e.g. GraphSnapshotId on ArchitectureRuns), use DbUp migrations or
      add the column once manually, then this script remains re-runnable.

  Aligns with DbUp migrations 001–007 (see ArchiForge.Data/Migrations/).

  Enable foreign keys per connection:
    PRAGMA foreign_keys = ON;

  GraphSnapshotId: TEXT (canonical GUID string), analogous to SQL Server UNIQUEIDENTIFIER.
  Date/time columns: TEXT (ISO-8601).
*/

CREATE TABLE IF NOT EXISTS ArchitectureRequests
(
    RequestId TEXT NOT NULL PRIMARY KEY,
    SystemName TEXT NOT NULL,
    Environment TEXT NOT NULL,
    CloudProvider TEXT NOT NULL,
    RequestJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ArchitectureRuns
(
    RunId TEXT NOT NULL PRIMARY KEY,
    RequestId TEXT NOT NULL,
    Status TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    CompletedUtc TEXT NULL,
    CurrentManifestVersion TEXT NULL,
    ContextSnapshotId TEXT NULL,
    GraphSnapshotId TEXT NULL,
    FOREIGN KEY (RequestId) REFERENCES ArchitectureRequests (RequestId)
);

CREATE INDEX IF NOT EXISTS IX_ArchitectureRuns_RequestId ON ArchitectureRuns (RequestId);
CREATE INDEX IF NOT EXISTS IX_ArchitectureRuns_CreatedUtc ON ArchitectureRuns (CreatedUtc DESC);
CREATE INDEX IF NOT EXISTS IX_ArchitectureRuns_ContextSnapshotId ON ArchitectureRuns (ContextSnapshotId);
CREATE INDEX IF NOT EXISTS IX_ArchitectureRuns_GraphSnapshotId ON ArchitectureRuns (GraphSnapshotId);

CREATE TABLE IF NOT EXISTS AgentTasks
(
    TaskId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    AgentType TEXT NOT NULL,
    Objective TEXT NOT NULL,
    Status TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    CompletedUtc TEXT NULL,
    EvidenceBundleRef TEXT NULL,
    FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IF NOT EXISTS IX_AgentTasks_RunId ON AgentTasks (RunId);
CREATE INDEX IF NOT EXISTS IX_AgentTasks_RunId_AgentType ON AgentTasks (RunId, AgentType);

CREATE TABLE IF NOT EXISTS AgentResults
(
    ResultId TEXT NOT NULL PRIMARY KEY,
    TaskId TEXT NOT NULL,
    RunId TEXT NOT NULL,
    AgentType TEXT NOT NULL,
    Confidence REAL NOT NULL,
    ResultJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    FOREIGN KEY (TaskId) REFERENCES AgentTasks (TaskId),
    FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IF NOT EXISTS IX_AgentResults_RunId ON AgentResults (RunId);
CREATE INDEX IF NOT EXISTS IX_AgentResults_TaskId ON AgentResults (TaskId);
CREATE INDEX IF NOT EXISTS IX_AgentResults_CreatedUtc ON AgentResults (CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS GoldenManifestVersions
(
    ManifestVersion TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    SystemName TEXT NOT NULL,
    ManifestJson TEXT NOT NULL,
    ParentManifestVersion TEXT NULL,
    CreatedUtc TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId),
    FOREIGN KEY (ParentManifestVersion) REFERENCES GoldenManifestVersions (ManifestVersion)
);

CREATE INDEX IF NOT EXISTS IX_GoldenManifestVersions_RunId ON GoldenManifestVersions (RunId);

CREATE TABLE IF NOT EXISTS EvidenceBundles
(
    EvidenceBundleId TEXT NOT NULL PRIMARY KEY,
    RequestDescription TEXT NOT NULL,
    EvidenceJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS DecisionTraces
(
    TraceId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    EventType TEXT NOT NULL,
    EventDescription TEXT NOT NULL,
    EventJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IF NOT EXISTS IX_DecisionTraces_RunId ON DecisionTraces (RunId);
CREATE INDEX IF NOT EXISTS IX_DecisionTraces_CreatedUtc ON DecisionTraces (CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS AgentEvidencePackages
(
    EvidencePackageId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    RequestId TEXT NOT NULL,
    SystemName TEXT NOT NULL,
    Environment TEXT NOT NULL,
    CloudProvider TEXT NOT NULL,
    EvidenceJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId),
    FOREIGN KEY (RequestId) REFERENCES ArchitectureRequests (RequestId)
);

CREATE INDEX IF NOT EXISTS IX_AgentEvidencePackages_RunId ON AgentEvidencePackages (RunId);

CREATE TABLE IF NOT EXISTS AgentExecutionTraces
(
    TraceId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    TaskId TEXT NOT NULL,
    AgentType TEXT NOT NULL,
    ParseSucceeded INTEGER NOT NULL,
    ErrorMessage TEXT NULL,
    TraceJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId),
    FOREIGN KEY (TaskId) REFERENCES AgentTasks (TaskId)
);

CREATE INDEX IF NOT EXISTS IX_AgentExecutionTraces_RunId ON AgentExecutionTraces (RunId);
CREATE INDEX IF NOT EXISTS IX_AgentExecutionTraces_TaskId ON AgentExecutionTraces (TaskId);

CREATE TABLE IF NOT EXISTS RunExportRecords
(
    ExportRecordId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    ExportType TEXT NOT NULL,
    Format TEXT NOT NULL,
    FileName TEXT NOT NULL,
    TemplateProfile TEXT NULL,
    TemplateProfileDisplayName TEXT NULL,
    WasAutoSelected INTEGER NOT NULL,
    ResolutionReason TEXT NULL,
    ManifestVersion TEXT NULL,
    Notes TEXT NULL,
    AnalysisRequestJson TEXT NULL,
    IncludedEvidence INTEGER NULL,
    IncludedExecutionTraces INTEGER NULL,
    IncludedManifest INTEGER NULL,
    IncludedDiagram INTEGER NULL,
    IncludedSummary INTEGER NULL,
    IncludedDeterminismCheck INTEGER NULL,
    DeterminismIterations INTEGER NULL,
    IncludedManifestCompare INTEGER NULL,
    CompareManifestVersion TEXT NULL,
    IncludedAgentResultCompare INTEGER NULL,
    CompareRunId TEXT NULL,
    RecordJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IF NOT EXISTS IX_RunExportRecords_RunId ON RunExportRecords (RunId);
CREATE INDEX IF NOT EXISTS IX_RunExportRecords_CreatedUtc ON RunExportRecords (CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS ComparisonRecords
(
    ComparisonRecordId TEXT NOT NULL PRIMARY KEY,
    ComparisonType TEXT NOT NULL,
    LeftRunId TEXT NULL,
    RightRunId TEXT NULL,
    LeftManifestVersion TEXT NULL,
    RightManifestVersion TEXT NULL,
    LeftExportRecordId TEXT NULL,
    RightExportRecordId TEXT NULL,
    Format TEXT NOT NULL,
    SummaryMarkdown TEXT NULL,
    PayloadJson TEXT NOT NULL,
    Notes TEXT NULL,
    CreatedUtc TEXT NOT NULL,
    Label TEXT NULL,
    Tags TEXT NULL,
    FOREIGN KEY (LeftRunId) REFERENCES ArchitectureRuns (RunId),
    FOREIGN KEY (RightRunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IF NOT EXISTS IX_ComparisonRecords_LeftRunId ON ComparisonRecords (LeftRunId);
CREATE INDEX IF NOT EXISTS IX_ComparisonRecords_RightRunId ON ComparisonRecords (RightRunId);
CREATE INDEX IF NOT EXISTS IX_ComparisonRecords_LeftExportRecordId ON ComparisonRecords (LeftExportRecordId);
CREATE INDEX IF NOT EXISTS IX_ComparisonRecords_RightExportRecordId ON ComparisonRecords (RightExportRecordId);
CREATE INDEX IF NOT EXISTS IX_ComparisonRecords_ComparisonType_CreatedUtc ON ComparisonRecords (ComparisonType, CreatedUtc DESC);
CREATE INDEX IF NOT EXISTS IX_ComparisonRecords_Label ON ComparisonRecords (Label) WHERE Label IS NOT NULL;

CREATE TABLE IF NOT EXISTS DecisionNodes
(
    DecisionId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    Topic TEXT NOT NULL,
    SelectedOptionId TEXT NULL,
    Confidence REAL NOT NULL,
    Rationale TEXT NOT NULL,
    DecisionJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId)
);

CREATE INDEX IF NOT EXISTS IX_DecisionNodes_RunId ON DecisionNodes (RunId);
CREATE INDEX IF NOT EXISTS IX_DecisionNodes_CreatedUtc ON DecisionNodes (CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS AgentEvaluations
(
    EvaluationId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    TargetAgentTaskId TEXT NOT NULL,
    EvaluationType TEXT NOT NULL,
    ConfidenceDelta REAL NOT NULL,
    Rationale TEXT NOT NULL,
    EvaluationJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES ArchitectureRuns (RunId),
    FOREIGN KEY (TargetAgentTaskId) REFERENCES AgentTasks (TaskId)
);

CREATE INDEX IF NOT EXISTS IX_AgentEvaluations_RunId ON AgentEvaluations (RunId);
CREATE INDEX IF NOT EXISTS IX_AgentEvaluations_TargetAgentTaskId ON AgentEvaluations (TargetAgentTaskId);
