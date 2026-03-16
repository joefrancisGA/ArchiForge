-- Initial schema for ArchiForge
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
    CurrentManifestVersion NVARCHAR(50) NULL
);

CREATE TABLE AgentTasks
(
    TaskId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    AgentType NVARCHAR(50) NOT NULL,
    Objective NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL,
    CompletedUtc DATETIME2 NULL,
    EvidenceBundleRef NVARCHAR(64) NULL
);

CREATE TABLE AgentResults
(
    ResultId NVARCHAR(64) NOT NULL PRIMARY KEY,
    TaskId NVARCHAR(64) NOT NULL,
    RunId NVARCHAR(64) NOT NULL,
    AgentType NVARCHAR(50) NOT NULL,
    Confidence FLOAT NOT NULL,
    ResultJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE TABLE GoldenManifestVersions
(
    ManifestVersion NVARCHAR(50) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    SystemName NVARCHAR(200) NOT NULL,
    ManifestJson NVARCHAR(MAX) NOT NULL,
    ParentManifestVersion NVARCHAR(50) NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE TABLE EvidenceBundles
(
    EvidenceBundleId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RequestDescription NVARCHAR(MAX) NOT NULL,
    EvidenceJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE TABLE DecisionTraces
(
    TraceId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    EventDescription NVARCHAR(MAX) NOT NULL,
    EventJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE TABLE AgentEvidencePackages
(
    EvidencePackageId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    RequestId NVARCHAR(64) NOT NULL,
    SystemName NVARCHAR(200) NOT NULL,
    Environment NVARCHAR(50) NOT NULL,
    CloudProvider NVARCHAR(50) NOT NULL,
    EvidenceJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_AgentEvidencePackages_RunId
    ON AgentEvidencePackages (RunId);

CREATE TABLE AgentExecutionTraces
(
    TraceId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    TaskId NVARCHAR(64) NOT NULL,
    AgentType NVARCHAR(50) NOT NULL,
    ParseSucceeded BIT NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    TraceJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_AgentExecutionTraces_RunId
    ON AgentExecutionTraces (RunId);

CREATE INDEX IX_AgentExecutionTraces_TaskId
    ON AgentExecutionTraces (TaskId);

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
    RecordJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_RunExportRecords_RunId
    ON RunExportRecords (RunId);
