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
    CurrentManifestVersion TEXT NULL
);

CREATE TABLE IF NOT EXISTS AgentTasks
(
    TaskId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    AgentType TEXT NOT NULL,
    Objective TEXT NOT NULL,
    Status TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    CompletedUtc TEXT NULL,
    EvidenceBundleRef TEXT NULL
);

CREATE TABLE IF NOT EXISTS AgentResults
(
    ResultId TEXT NOT NULL PRIMARY KEY,
    TaskId TEXT NOT NULL,
    RunId TEXT NOT NULL,
    AgentType TEXT NOT NULL,
    Confidence REAL NOT NULL,
    ResultJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS GoldenManifestVersions
(
    ManifestVersion TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    SystemName TEXT NOT NULL,
    ManifestJson TEXT NOT NULL,
    ParentManifestVersion TEXT NULL,
    CreatedUtc TEXT NOT NULL
);

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
    CreatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS AgentEvidencePackages
(
    EvidencePackageId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    RequestId TEXT NOT NULL,
    SystemName TEXT NOT NULL,
    Environment TEXT NOT NULL,
    CloudProvider TEXT NOT NULL,
    EvidenceJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL
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
    CreatedUtc TEXT NOT NULL
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
    CreatedUtc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_RunExportRecords_RunId ON RunExportRecords (RunId);

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
    CreatedUtc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_ComparisonRecords_LeftRunId ON ComparisonRecords (LeftRunId);
CREATE INDEX IF NOT EXISTS IX_ComparisonRecords_RightRunId ON ComparisonRecords (RightRunId);
CREATE INDEX IF NOT EXISTS IX_ComparisonRecords_LeftExportRecordId ON ComparisonRecords (LeftExportRecordId);
CREATE INDEX IF NOT EXISTS IX_ComparisonRecords_RightExportRecordId ON ComparisonRecords (RightExportRecordId);
