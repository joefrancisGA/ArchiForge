/*
  ArchiForge — SQLite consolidated schema (tests / local / reference)

  Idempotency:
    - CREATE TABLE IF NOT EXISTS / CREATE INDEX IF NOT EXISTS → safe to run repeatedly.
    - Each table’s indexes follow immediately after that table’s CREATE (SQLite has no inline
      INDEX … syntax for arbitrary non-unique indexes, unlike SQL Server’s CREATE TABLE).
    - All columns are defined on CREATE (same principle as ArchiForge.sql). Older DB files
      that predate a column are not upgraded by this script — use a migration path or a new
      database for tests.

  Aligns with DbUp migrations 001–016 and the authority / decisioning section of
  ArchiForge.sql (GUID Runs, recommendations, advisory, digests, alerts, policy packs).

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
    ArtifactBundleId TEXT NULL,
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

/* ---- Authority / Dapper persistence + Decisioning (GUID RunId; not ArchitectureRuns) ---- */

CREATE TABLE IF NOT EXISTS Runs
(
    RunId TEXT NOT NULL PRIMARY KEY,
    ProjectId TEXT NOT NULL,
    Description TEXT NULL,
    CreatedUtc TEXT NOT NULL,
    ContextSnapshotId TEXT NULL,
    GraphSnapshotId TEXT NULL,
    FindingsSnapshotId TEXT NULL,
    GoldenManifestId TEXT NULL,
    DecisionTraceId TEXT NULL,
    ArtifactBundleId TEXT NULL,
    TenantId TEXT NOT NULL DEFAULT '11111111-1111-1111-1111-111111111111',
    WorkspaceId TEXT NOT NULL DEFAULT '22222222-2222-2222-2222-222222222222',
    ScopeProjectId TEXT NOT NULL DEFAULT '33333333-3333-3333-3333-333333333333'
);

CREATE INDEX IF NOT EXISTS IX_Runs_ProjectId_CreatedUtc ON Runs (ProjectId, CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS ContextSnapshots
(
    SnapshotId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    CanonicalObjectsJson TEXT NOT NULL,
    DeltaSummary TEXT NULL,
    WarningsJson TEXT NOT NULL,
    ErrorsJson TEXT NOT NULL,
    SourceHashesJson TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES Runs (RunId)
);

CREATE INDEX IF NOT EXISTS IX_ContextSnapshots_ProjectId_CreatedUtc ON ContextSnapshots (ProjectId, CreatedUtc DESC);
CREATE INDEX IF NOT EXISTS IX_ContextSnapshots_RunId ON ContextSnapshots (RunId);

CREATE TABLE IF NOT EXISTS GraphSnapshots
(
    GraphSnapshotId TEXT NOT NULL PRIMARY KEY,
    ContextSnapshotId TEXT NOT NULL,
    RunId TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    NodesJson TEXT NOT NULL,
    EdgesJson TEXT NOT NULL,
    WarningsJson TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES Runs (RunId),
    FOREIGN KEY (ContextSnapshotId) REFERENCES ContextSnapshots (SnapshotId)
);

CREATE INDEX IF NOT EXISTS IX_GraphSnapshots_RunId ON GraphSnapshots (RunId);
CREATE INDEX IF NOT EXISTS IX_GraphSnapshots_ContextSnapshotId ON GraphSnapshots (ContextSnapshotId);

CREATE TABLE IF NOT EXISTS FindingsSnapshots
(
    FindingsSnapshotId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    ContextSnapshotId TEXT NOT NULL,
    GraphSnapshotId TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    FindingsJson TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES Runs (RunId),
    FOREIGN KEY (ContextSnapshotId) REFERENCES ContextSnapshots (SnapshotId),
    FOREIGN KEY (GraphSnapshotId) REFERENCES GraphSnapshots (GraphSnapshotId)
);

CREATE INDEX IF NOT EXISTS IX_FindingsSnapshots_RunId ON FindingsSnapshots (RunId);
CREATE INDEX IF NOT EXISTS IX_FindingsSnapshots_ContextSnapshotId ON FindingsSnapshots (ContextSnapshotId);
CREATE INDEX IF NOT EXISTS IX_FindingsSnapshots_GraphSnapshotId ON FindingsSnapshots (GraphSnapshotId);

CREATE TABLE IF NOT EXISTS DecisioningTraces
(
    DecisionTraceId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    RuleSetId TEXT NOT NULL,
    RuleSetVersion TEXT NOT NULL,
    RuleSetHash TEXT NOT NULL,
    AppliedRuleIdsJson TEXT NOT NULL,
    AcceptedFindingIdsJson TEXT NOT NULL,
    RejectedFindingIdsJson TEXT NOT NULL,
    NotesJson TEXT NOT NULL,
    TenantId TEXT NOT NULL DEFAULT '11111111-1111-1111-1111-111111111111',
    WorkspaceId TEXT NOT NULL DEFAULT '22222222-2222-2222-2222-222222222222',
    ProjectId TEXT NOT NULL DEFAULT '33333333-3333-3333-3333-333333333333',
    FOREIGN KEY (RunId) REFERENCES Runs (RunId)
);

CREATE INDEX IF NOT EXISTS IX_DecisioningTraces_RunId ON DecisioningTraces (RunId);

CREATE TABLE IF NOT EXISTS GoldenManifests
(
    ManifestId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    ContextSnapshotId TEXT NOT NULL,
    GraphSnapshotId TEXT NOT NULL,
    FindingsSnapshotId TEXT NOT NULL,
    DecisionTraceId TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    ManifestHash TEXT NOT NULL,
    RuleSetId TEXT NOT NULL,
    RuleSetVersion TEXT NOT NULL,
    RuleSetHash TEXT NOT NULL,
    MetadataJson TEXT NOT NULL,
    RequirementsJson TEXT NOT NULL,
    TopologyJson TEXT NOT NULL,
    SecurityJson TEXT NOT NULL,
    ComplianceJson TEXT NOT NULL,
    CostJson TEXT NOT NULL,
    ConstraintsJson TEXT NOT NULL,
    UnresolvedIssuesJson TEXT NOT NULL,
    DecisionsJson TEXT NOT NULL,
    AssumptionsJson TEXT NOT NULL,
    WarningsJson TEXT NOT NULL,
    ProvenanceJson TEXT NOT NULL,
    TenantId TEXT NOT NULL DEFAULT '11111111-1111-1111-1111-111111111111',
    WorkspaceId TEXT NOT NULL DEFAULT '22222222-2222-2222-2222-222222222222',
    ProjectId TEXT NOT NULL DEFAULT '33333333-3333-3333-3333-333333333333',
    FOREIGN KEY (RunId) REFERENCES Runs (RunId),
    FOREIGN KEY (ContextSnapshotId) REFERENCES ContextSnapshots (SnapshotId),
    FOREIGN KEY (GraphSnapshotId) REFERENCES GraphSnapshots (GraphSnapshotId),
    FOREIGN KEY (FindingsSnapshotId) REFERENCES FindingsSnapshots (FindingsSnapshotId),
    FOREIGN KEY (DecisionTraceId) REFERENCES DecisioningTraces (DecisionTraceId)
);

CREATE INDEX IF NOT EXISTS IX_GoldenManifests_RunId ON GoldenManifests (RunId);

CREATE TABLE IF NOT EXISTS ArtifactBundles
(
    BundleId TEXT NOT NULL PRIMARY KEY,
    RunId TEXT NOT NULL,
    ManifestId TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    ArtifactsJson TEXT NOT NULL,
    TraceJson TEXT NOT NULL,
    TenantId TEXT NOT NULL DEFAULT '11111111-1111-1111-1111-111111111111',
    WorkspaceId TEXT NOT NULL DEFAULT '22222222-2222-2222-2222-222222222222',
    ProjectId TEXT NOT NULL DEFAULT '33333333-3333-3333-3333-333333333333',
    FOREIGN KEY (RunId) REFERENCES Runs (RunId),
    FOREIGN KEY (ManifestId) REFERENCES GoldenManifests (ManifestId)
);

CREATE INDEX IF NOT EXISTS IX_ArtifactBundles_RunId ON ArtifactBundles (RunId);
CREATE INDEX IF NOT EXISTS IX_ArtifactBundles_ManifestId ON ArtifactBundles (ManifestId);

CREATE TABLE IF NOT EXISTS AuditEvents
(
    EventId TEXT NOT NULL PRIMARY KEY,
    OccurredUtc TEXT NOT NULL,
    EventType TEXT NOT NULL,
    ActorUserId TEXT NOT NULL,
    ActorUserName TEXT NOT NULL,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    RunId TEXT NULL,
    ManifestId TEXT NULL,
    ArtifactId TEXT NULL,
    DataJson TEXT NOT NULL,
    CorrelationId TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_AuditEvents_Scope_OccurredUtc ON AuditEvents (TenantId, WorkspaceId, ProjectId, OccurredUtc DESC);

CREATE TABLE IF NOT EXISTS ProvenanceSnapshots
(
    Id TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    RunId TEXT NOT NULL,
    GraphJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES Runs (RunId)
);

CREATE INDEX IF NOT EXISTS IX_ProvenanceSnapshots_Scope_Run ON ProvenanceSnapshots (TenantId, WorkspaceId, ProjectId, RunId, CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS ConversationThreads
(
    ThreadId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    RunId TEXT NULL,
    BaseRunId TEXT NULL,
    TargetRunId TEXT NULL,
    Title TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    LastUpdatedUtc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_ConversationThreads_Scope ON ConversationThreads (TenantId, WorkspaceId, ProjectId, LastUpdatedUtc DESC);

CREATE TABLE IF NOT EXISTS ConversationMessages
(
    MessageId TEXT NOT NULL PRIMARY KEY,
    ThreadId TEXT NOT NULL,
    Role TEXT NOT NULL,
    Content TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    MetadataJson TEXT NOT NULL,
    FOREIGN KEY (ThreadId) REFERENCES ConversationThreads (ThreadId)
);

CREATE INDEX IF NOT EXISTS IX_ConversationMessages_ThreadId_CreatedUtc ON ConversationMessages (ThreadId, CreatedUtc ASC);

CREATE TABLE IF NOT EXISTS RecommendationRecords
(
    RecommendationId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    RunId TEXT NOT NULL,
    ComparedToRunId TEXT NULL,
    Title TEXT NOT NULL,
    Category TEXT NOT NULL,
    Rationale TEXT NOT NULL,
    SuggestedAction TEXT NOT NULL,
    Urgency TEXT NOT NULL,
    ExpectedImpact TEXT NOT NULL,
    PriorityScore INTEGER NOT NULL,
    Status TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    LastUpdatedUtc TEXT NOT NULL,
    ReviewedByUserId TEXT NULL,
    ReviewedByUserName TEXT NULL,
    ReviewComment TEXT NULL,
    ResolutionRationale TEXT NULL,
    SupportingFindingIdsJson TEXT NOT NULL,
    SupportingDecisionIdsJson TEXT NOT NULL,
    SupportingArtifactIdsJson TEXT NOT NULL,
    FOREIGN KEY (RunId) REFERENCES Runs (RunId)
);

CREATE INDEX IF NOT EXISTS IX_RecommendationRecords_Scope_Run ON RecommendationRecords (TenantId, WorkspaceId, ProjectId, RunId, CreatedUtc DESC);
CREATE INDEX IF NOT EXISTS IX_RecommendationRecords_Scope_Status ON RecommendationRecords (TenantId, WorkspaceId, ProjectId, Status, LastUpdatedUtc DESC);

CREATE TABLE IF NOT EXISTS RecommendationLearningProfiles
(
    ProfileId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    GeneratedUtc TEXT NOT NULL,
    ProfileJson TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_RecommendationLearningProfiles_Scope_GeneratedUtc ON RecommendationLearningProfiles (TenantId, WorkspaceId, ProjectId, GeneratedUtc DESC);

CREATE TABLE IF NOT EXISTS AdvisoryScanSchedules
(
    ScheduleId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    RunProjectSlug TEXT NOT NULL DEFAULT 'default',
    Name TEXT NOT NULL,
    CronExpression TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL,
    CreatedUtc TEXT NOT NULL,
    LastRunUtc TEXT NULL,
    NextRunUtc TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_AdvisoryScanSchedules_Scope_Enabled_NextRun ON AdvisoryScanSchedules (TenantId, WorkspaceId, ProjectId, IsEnabled, NextRunUtc);

CREATE TABLE IF NOT EXISTS AdvisoryScanExecutions
(
    ExecutionId TEXT NOT NULL PRIMARY KEY,
    ScheduleId TEXT NOT NULL,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    StartedUtc TEXT NOT NULL,
    CompletedUtc TEXT NULL,
    Status TEXT NOT NULL,
    ResultJson TEXT NOT NULL,
    ErrorMessage TEXT NULL,
    FOREIGN KEY (ScheduleId) REFERENCES AdvisoryScanSchedules (ScheduleId)
);

CREATE INDEX IF NOT EXISTS IX_AdvisoryScanExecutions_Schedule_StartedUtc ON AdvisoryScanExecutions (ScheduleId, StartedUtc DESC);

CREATE TABLE IF NOT EXISTS ArchitectureDigests
(
    DigestId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    RunId TEXT NULL,
    ComparedToRunId TEXT NULL,
    GeneratedUtc TEXT NOT NULL,
    Title TEXT NOT NULL,
    Summary TEXT NOT NULL,
    ContentMarkdown TEXT NOT NULL,
    MetadataJson TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_ArchitectureDigests_Scope_GeneratedUtc ON ArchitectureDigests (TenantId, WorkspaceId, ProjectId, GeneratedUtc DESC);

CREATE TABLE IF NOT EXISTS DigestSubscriptions
(
    SubscriptionId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    Name TEXT NOT NULL,
    ChannelType TEXT NOT NULL,
    Destination TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL,
    CreatedUtc TEXT NOT NULL,
    LastDeliveredUtc TEXT NULL,
    MetadataJson TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_DigestSubscriptions_Scope_Enabled ON DigestSubscriptions (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS DigestDeliveryAttempts
(
    AttemptId TEXT NOT NULL PRIMARY KEY,
    DigestId TEXT NOT NULL,
    SubscriptionId TEXT NOT NULL,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    AttemptedUtc TEXT NOT NULL,
    Status TEXT NOT NULL,
    ErrorMessage TEXT NULL,
    ChannelType TEXT NOT NULL,
    Destination TEXT NOT NULL,
    FOREIGN KEY (DigestId) REFERENCES ArchitectureDigests (DigestId),
    FOREIGN KEY (SubscriptionId) REFERENCES DigestSubscriptions (SubscriptionId)
);

CREATE INDEX IF NOT EXISTS IX_DigestDeliveryAttempts_DigestId_AttemptedUtc ON DigestDeliveryAttempts (DigestId, AttemptedUtc DESC);
CREATE INDEX IF NOT EXISTS IX_DigestDeliveryAttempts_SubscriptionId_AttemptedUtc ON DigestDeliveryAttempts (SubscriptionId, AttemptedUtc DESC);

CREATE TABLE IF NOT EXISTS AlertRules
(
    RuleId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    Name TEXT NOT NULL,
    RuleType TEXT NOT NULL,
    Severity TEXT NOT NULL,
    ThresholdValue REAL NOT NULL,
    IsEnabled INTEGER NOT NULL,
    TargetChannelType TEXT NOT NULL,
    MetadataJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_AlertRules_Scope_Enabled ON AlertRules (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS AlertRecords
(
    AlertId TEXT NOT NULL PRIMARY KEY,
    RuleId TEXT NOT NULL,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    RunId TEXT NULL,
    ComparedToRunId TEXT NULL,
    RecommendationId TEXT NULL,
    Title TEXT NOT NULL,
    Category TEXT NOT NULL,
    Severity TEXT NOT NULL,
    Status TEXT NOT NULL,
    TriggerValue TEXT NOT NULL,
    Description TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    LastUpdatedUtc TEXT NULL,
    AcknowledgedByUserId TEXT NULL,
    AcknowledgedByUserName TEXT NULL,
    ResolutionComment TEXT NULL,
    DeduplicationKey TEXT NOT NULL,
    FOREIGN KEY (RuleId) REFERENCES AlertRules (RuleId)
);

CREATE INDEX IF NOT EXISTS IX_AlertRecords_Scope_Status_CreatedUtc ON AlertRecords (TenantId, WorkspaceId, ProjectId, Status, CreatedUtc DESC);
CREATE INDEX IF NOT EXISTS IX_AlertRecords_DeduplicationKey ON AlertRecords (DeduplicationKey);

CREATE TABLE IF NOT EXISTS AlertRoutingSubscriptions
(
    RoutingSubscriptionId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    Name TEXT NOT NULL,
    ChannelType TEXT NOT NULL,
    Destination TEXT NOT NULL,
    MinimumSeverity TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL,
    CreatedUtc TEXT NOT NULL,
    LastDeliveredUtc TEXT NULL,
    MetadataJson TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_AlertRoutingSubscriptions_Scope_Enabled ON AlertRoutingSubscriptions (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS AlertDeliveryAttempts
(
    AlertDeliveryAttemptId TEXT NOT NULL PRIMARY KEY,
    AlertId TEXT NOT NULL,
    RoutingSubscriptionId TEXT NOT NULL,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    AttemptedUtc TEXT NOT NULL,
    Status TEXT NOT NULL,
    ErrorMessage TEXT NULL,
    ChannelType TEXT NOT NULL,
    Destination TEXT NOT NULL,
    RetryCount INTEGER NOT NULL,
    FOREIGN KEY (AlertId) REFERENCES AlertRecords (AlertId),
    FOREIGN KEY (RoutingSubscriptionId) REFERENCES AlertRoutingSubscriptions (RoutingSubscriptionId)
);

CREATE INDEX IF NOT EXISTS IX_AlertDeliveryAttempts_AlertId_AttemptedUtc ON AlertDeliveryAttempts (AlertId, AttemptedUtc DESC);
CREATE INDEX IF NOT EXISTS IX_AlertDeliveryAttempts_RoutingSubscriptionId_AttemptedUtc ON AlertDeliveryAttempts (RoutingSubscriptionId, AttemptedUtc DESC);

CREATE TABLE IF NOT EXISTS CompositeAlertRules
(
    CompositeRuleId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Severity TEXT NOT NULL,
    "Operator" TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL,
    SuppressionWindowMinutes INTEGER NOT NULL,
    CooldownMinutes INTEGER NOT NULL,
    ReopenDeltaThreshold REAL NOT NULL,
    DedupeScope TEXT NOT NULL,
    TargetChannelType TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_CompositeAlertRules_Scope_Enabled ON CompositeAlertRules (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS CompositeAlertRuleConditions
(
    ConditionId TEXT NOT NULL PRIMARY KEY,
    CompositeRuleId TEXT NOT NULL,
    MetricType TEXT NOT NULL,
    "Operator" TEXT NOT NULL,
    ThresholdValue REAL NOT NULL,
    FOREIGN KEY (CompositeRuleId) REFERENCES CompositeAlertRules (CompositeRuleId)
);

CREATE INDEX IF NOT EXISTS IX_CompositeAlertRuleConditions_CompositeRuleId ON CompositeAlertRuleConditions (CompositeRuleId);

CREATE TABLE IF NOT EXISTS PolicyPacks
(
    PolicyPackId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Description TEXT NOT NULL,
    PackType TEXT NOT NULL,
    Status TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    ActivatedUtc TEXT NULL,
    CurrentVersion TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_PolicyPacks_Scope_Status ON PolicyPacks (TenantId, WorkspaceId, ProjectId, Status, CreatedUtc DESC);

CREATE TABLE IF NOT EXISTS PolicyPackVersions
(
    PolicyPackVersionId TEXT NOT NULL PRIMARY KEY,
    PolicyPackId TEXT NOT NULL,
    Version TEXT NOT NULL,
    ContentJson TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    IsPublished INTEGER NOT NULL,
    FOREIGN KEY (PolicyPackId) REFERENCES PolicyPacks (PolicyPackId)
);

CREATE INDEX IF NOT EXISTS IX_PolicyPackVersions_PolicyPackId_Version ON PolicyPackVersions (PolicyPackId, Version);

CREATE TABLE IF NOT EXISTS PolicyPackAssignments
(
    AssignmentId TEXT NOT NULL PRIMARY KEY,
    TenantId TEXT NOT NULL,
    WorkspaceId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    PolicyPackId TEXT NOT NULL,
    PolicyPackVersion TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL,
    ScopeLevel TEXT NOT NULL DEFAULT 'Project',
    IsPinned INTEGER NOT NULL DEFAULT 0,
    AssignedUtc TEXT NOT NULL,
    FOREIGN KEY (PolicyPackId) REFERENCES PolicyPacks (PolicyPackId)
);

CREATE INDEX IF NOT EXISTS IX_PolicyPackAssignments_Scope_Enabled ON PolicyPackAssignments (TenantId, WorkspaceId, ProjectId, IsEnabled, AssignedUtc DESC);
CREATE INDEX IF NOT EXISTS IX_PolicyPackAssignments_ScopeLevel_AssignedUtc ON PolicyPackAssignments (TenantId, WorkspaceId, ProjectId, ScopeLevel, AssignedUtc DESC);
