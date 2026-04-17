/*
  ArchLucid — greenfield baseline (DbUp)
  Mechanical union of forward migrations 001..050 as of 2026-04-17.
  Do not edit by hand; regenerate from Migrations/*.sql when 001..050 change.
  Brownfield catalogs skip this file (see GreenfieldBaselineMigrationRunner).
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---- 001_InitialSchema.sql ---- */
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
    CreatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_RunExportRecords_RunId
    ON RunExportRecords (RunId);

GO

/* ---- 002_ComparisonRecords.sql ---- */
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
    CreatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_ComparisonRecords_LeftRunId ON ComparisonRecords (LeftRunId);
CREATE INDEX IX_ComparisonRecords_RightRunId ON ComparisonRecords (RightRunId);
CREATE INDEX IX_ComparisonRecords_LeftExportRecordId ON ComparisonRecords (LeftExportRecordId);
CREATE INDEX IX_ComparisonRecords_RightExportRecordId ON ComparisonRecords (RightExportRecordId);


GO

/* ---- 003_ComparisonRecords_LabelAndTags.sql ---- */
ALTER TABLE ComparisonRecords
ADD Label NVARCHAR(256) NULL,
    Tags NVARCHAR(MAX) NULL;

GO

/* ---- 004_DecisionNodes_And_Evaluations.sql ---- */
-- Decision nodes (Decision Engine v2 output) + agent evaluations

CREATE TABLE DecisionNodes
(
    DecisionId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    Topic NVARCHAR(100) NOT NULL,
    SelectedOptionId NVARCHAR(64) NULL,
    Confidence FLOAT NOT NULL,
    Rationale NVARCHAR(MAX) NOT NULL,
    DecisionJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_DecisionNodes_RunId
    ON DecisionNodes (RunId);

CREATE TABLE AgentEvaluations
(
    EvaluationId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    TargetAgentTaskId NVARCHAR(64) NOT NULL,
    EvaluationType NVARCHAR(50) NOT NULL,
    ConfidenceDelta FLOAT NOT NULL,
    Rationale NVARCHAR(MAX) NOT NULL,
    EvaluationJson NVARCHAR(MAX) NOT NULL,
    CreatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_AgentEvaluations_RunId
    ON AgentEvaluations (RunId);


GO

/* ---- 005_ArchitectureRuns_ContextSnapshotId.sql ---- */
-- Add context snapshot reference to runs
ALTER TABLE ArchitectureRuns
    ADD ContextSnapshotId NVARCHAR(64) NULL;


GO

/* ---- 006_ArchitectureRuns_GraphSnapshotId.sql ---- */
-- Add graph snapshot reference to runs
ALTER TABLE ArchitectureRuns
    ADD GraphSnapshotId UNIQUEIDENTIFIER NULL;


GO

/* ---- 007_ArchitectureRuns_ArtifactBundleId.sql ---- */
ALTER TABLE ArchitectureRuns
    ADD ArtifactBundleId UNIQUEIDENTIFIER NULL;

GO

/* ---- 008_RecommendationRecords.sql ---- */
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
        SupportingArtifactIdsJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_RecommendationRecords_Scope_Run
        ON dbo.RecommendationRecords (TenantId, WorkspaceId, ProjectId, RunId, CreatedUtc DESC);

    CREATE NONCLUSTERED INDEX IX_RecommendationRecords_Scope_Status
        ON dbo.RecommendationRecords (TenantId, WorkspaceId, ProjectId, Status, LastUpdatedUtc DESC);
END

GO

/* ---- 009_RecommendationLearningProfiles.sql ---- */
IF OBJECT_ID('dbo.RecommendationLearningProfiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecommendationLearningProfiles
    (
        ProfileId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        GeneratedUtc DATETIME2 NOT NULL,
        ProfileJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_RecommendationLearningProfiles_Scope_GeneratedUtc
        ON dbo.RecommendationLearningProfiles (TenantId, WorkspaceId, ProjectId, GeneratedUtc DESC);
END

GO

/* ---- 010_AdvisoryScheduling.sql ---- */
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
        NextRunUtc DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX IX_AdvisoryScanSchedules_Scope_Enabled_NextRun
        ON dbo.AdvisoryScanSchedules (TenantId, WorkspaceId, ProjectId, IsEnabled, NextRunUtc);
END

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
        ErrorMessage NVARCHAR(MAX) NULL
    );

    CREATE NONCLUSTERED INDEX IX_AdvisoryScanExecutions_Schedule_StartedUtc
        ON dbo.AdvisoryScanExecutions (ScheduleId, StartedUtc DESC);
END

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
        MetadataJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_ArchitectureDigests_Scope_GeneratedUtc
        ON dbo.ArchitectureDigests (TenantId, WorkspaceId, ProjectId, GeneratedUtc DESC);
END

GO

/* ---- 011_DigestDelivery.sql ---- */
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
        MetadataJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_DigestSubscriptions_Scope_Enabled
        ON dbo.DigestSubscriptions (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC);
END

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
        Destination NVARCHAR(1000) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_DigestDeliveryAttempts_DigestId_AttemptedUtc
        ON dbo.DigestDeliveryAttempts (DigestId, AttemptedUtc DESC);

    CREATE NONCLUSTERED INDEX IX_DigestDeliveryAttempts_SubscriptionId_AttemptedUtc
        ON dbo.DigestDeliveryAttempts (SubscriptionId, AttemptedUtc DESC);
END

GO

/* ---- 012_Alerts.sql ---- */
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
        CreatedUtc DATETIME2 NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_AlertRules_Scope_Enabled
        ON dbo.AlertRules (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC);
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
        DeduplicationKey NVARCHAR(500) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_AlertRecords_Scope_Status_CreatedUtc
        ON dbo.AlertRecords (TenantId, WorkspaceId, ProjectId, Status, CreatedUtc DESC);

    CREATE NONCLUSTERED INDEX IX_AlertRecords_DeduplicationKey
        ON dbo.AlertRecords (DeduplicationKey);
END;
GO

GO

/* ---- 013_AlertRouting.sql ---- */
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
        MetadataJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_AlertRoutingSubscriptions_Scope_Enabled
        ON dbo.AlertRoutingSubscriptions (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC);
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
        RetryCount INT NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_AlertDeliveryAttempts_AlertId_AttemptedUtc
        ON dbo.AlertDeliveryAttempts (AlertId, AttemptedUtc DESC);

    CREATE NONCLUSTERED INDEX IX_AlertDeliveryAttempts_RoutingSubscriptionId_AttemptedUtc
        ON dbo.AlertDeliveryAttempts (RoutingSubscriptionId, AttemptedUtc DESC);
END;
GO

GO

/* ---- 014_CompositeAlertRules.sql ---- */
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
        CreatedUtc DATETIME2 NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_CompositeAlertRules_Scope_Enabled
        ON dbo.CompositeAlertRules (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC);
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
        ThresholdValue DECIMAL(18, 4) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_CompositeAlertRuleConditions_CompositeRuleId
        ON dbo.CompositeAlertRuleConditions (CompositeRuleId);
END;
GO

GO

/* ---- 015_PolicyPacks.sql ---- */
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
        CurrentVersion NVARCHAR(50) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_PolicyPacks_Scope_Status
        ON dbo.PolicyPacks (TenantId, WorkspaceId, ProjectId, Status, CreatedUtc DESC);
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
        IsPublished BIT NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_PolicyPackVersions_PolicyPackId_Version
        ON dbo.PolicyPackVersions (PolicyPackId, [Version]);
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
        AssignedUtc DATETIME2 NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_PolicyPackAssignments_Scope_Enabled
        ON dbo.PolicyPackAssignments (TenantId, WorkspaceId, ProjectId, IsEnabled, AssignedUtc DESC);
END;
GO

GO

/* ---- 016_PolicyPackAssignments_Scope.sql ---- */
/* Align DbUp-only databases with Dapper: ScopeLevel + IsPinned on PolicyPackAssignments. */

IF OBJECT_ID('dbo.PolicyPackAssignments', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PolicyPackAssignments', 'ScopeLevel') IS NULL
    BEGIN
        ALTER TABLE dbo.PolicyPackAssignments ADD ScopeLevel NVARCHAR(50) NOT NULL
            CONSTRAINT DF_PolicyPackAssignments_ScopeLevel DEFAULT (N'Project');
    END;

    IF COL_LENGTH('dbo.PolicyPackAssignments', 'IsPinned') IS NULL
    BEGIN
        ALTER TABLE dbo.PolicyPackAssignments ADD IsPinned BIT NOT NULL
            CONSTRAINT DF_PolicyPackAssignments_IsPinned DEFAULT (0);
    END;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PolicyPackAssignments_ScopeLevel_AssignedUtc'
      AND object_id = OBJECT_ID(N'dbo.PolicyPackAssignments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PolicyPackAssignments_ScopeLevel_AssignedUtc
        ON dbo.PolicyPackAssignments (TenantId, WorkspaceId, ProjectId, ScopeLevel, AssignedUtc DESC);
END;
GO

GO

/* ---- 017_GovernanceWorkflow.sql ---- */
CREATE TABLE GovernanceApprovalRequests (
    ApprovalRequestId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    ManifestVersion NVARCHAR(128) NOT NULL,
    SourceEnvironment NVARCHAR(32) NOT NULL,
    TargetEnvironment NVARCHAR(32) NOT NULL,
    Status NVARCHAR(32) NOT NULL,
    RequestedBy NVARCHAR(200) NOT NULL,
    ReviewedBy NVARCHAR(200) NULL,
    RequestComment NVARCHAR(MAX) NULL,
    ReviewComment NVARCHAR(MAX) NULL,
    RequestedUtc DATETIME2 NOT NULL,
    ReviewedUtc DATETIME2 NULL
);

CREATE TABLE GovernancePromotionRecords (
    PromotionRecordId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    ManifestVersion NVARCHAR(128) NOT NULL,
    SourceEnvironment NVARCHAR(32) NOT NULL,
    TargetEnvironment NVARCHAR(32) NOT NULL,
    PromotedBy NVARCHAR(200) NOT NULL,
    PromotedUtc DATETIME2 NOT NULL,
    ApprovalRequestId NVARCHAR(64) NULL,
    Notes NVARCHAR(MAX) NULL
);

CREATE TABLE GovernanceEnvironmentActivations (
    ActivationId NVARCHAR(64) NOT NULL PRIMARY KEY,
    RunId NVARCHAR(64) NOT NULL,
    ManifestVersion NVARCHAR(128) NOT NULL,
    Environment NVARCHAR(32) NOT NULL,
    IsActive BIT NOT NULL,
    ActivatedUtc DATETIME2 NOT NULL
);

CREATE INDEX IX_GovernanceApprovalRequests_RunId
ON GovernanceApprovalRequests(RunId);

CREATE INDEX IX_GovernancePromotionRecords_RunId
ON GovernancePromotionRecords(RunId);

CREATE INDEX IX_GovernanceEnvironmentActivations_Environment_IsActive
ON GovernanceEnvironmentActivations(Environment, IsActive);

GO

/* ---- 017_GraphSnapshots_ParentTables.sql ---- */
-- Parent tables for dbo.GraphSnapshotEdges (FK in 018). Authority/decisioning persistence; aligns with SQL/ArchiForge.sql.
IF OBJECT_ID(N'dbo.Runs', N'U') IS NULL
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
            CONSTRAINT DF_Runs_ScopeProjectId DEFAULT ('33333333-3333-3333-3333-333333333333')
    );
    CREATE NONCLUSTERED INDEX IX_Runs_ProjectId_CreatedUtc ON dbo.Runs (ProjectId, CreatedUtc DESC);
END;

IF OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NULL
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
        SourceHashesJson NVARCHAR(MAX) NOT NULL
    );
    CREATE NONCLUSTERED INDEX IX_ContextSnapshots_ProjectId_CreatedUtc ON dbo.ContextSnapshots (ProjectId, CreatedUtc DESC);
    CREATE NONCLUSTERED INDEX IX_ContextSnapshots_RunId ON dbo.ContextSnapshots (RunId);
END;

IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GraphSnapshots
    (
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ContextSnapshotId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        NodesJson NVARCHAR(MAX) NOT NULL,
        EdgesJson NVARCHAR(MAX) NOT NULL,
        WarningsJson NVARCHAR(MAX) NOT NULL
    );
    CREATE NONCLUSTERED INDEX IX_GraphSnapshots_RunId ON dbo.GraphSnapshots (RunId);
    CREATE NONCLUSTERED INDEX IX_GraphSnapshots_ContextSnapshotId ON dbo.GraphSnapshots (ContextSnapshotId);
END;

GO

/* ---- 018_GraphSnapshotEdges.sql ---- */
-- Denormalized edges for indexed queries without deserializing EdgesJson.
IF OBJECT_ID(N'dbo.GraphSnapshotEdges', N'U') IS NULL
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

    -- Index key (GraphSnapshotId, FromNodeId, ToNodeId) exceeds 1700-byte SQL Server limit; use INCLUDE for ToNodeId.
    CREATE NONCLUSTERED INDEX IX_GraphSnapshotEdges_SnapshotFrom
        ON dbo.GraphSnapshotEdges (GraphSnapshotId, FromNodeId)
        INCLUDE (ToNodeId, EdgeType, Weight);
END;

GO

/* ---- 019_RetrievalIndexingOutbox.sql ---- */
-- Post-commit queue for retrieval (RAG) indexing; processed by RetrievalIndexingOutboxProcessorHostedService.
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
END
GO

GO

/* ---- 020_PerformanceIndexes_HotLists.sql ---- */
-- Â§227: Support scoped list queries on authority Runs (ListByProjectAsync: TenantId, WorkspaceId, ScopeProjectId, ProjectId, ORDER BY CreatedUtc DESC).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Runs_Scope_Project_CreatedUtc'
      AND object_id = OBJECT_ID(N'dbo.Runs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Runs_Scope_Project_CreatedUtc
        ON dbo.Runs (TenantId, WorkspaceId, ScopeProjectId, ProjectId, CreatedUtc DESC);
END
GO

GO

/* ---- 021_ArchitectureRunIdempotency.sql ---- */
-- Optional Idempotency-Key on POST /architecture/request: one row per (scope, key hash).
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
        CONSTRAINT PK_ArchitectureRunIdempotency PRIMARY KEY (TenantId, WorkspaceId, ProjectId, IdempotencyKeyHash),
        CONSTRAINT FK_ArchitectureRunIdempotency_Run FOREIGN KEY (RunId) REFERENCES dbo.ArchitectureRuns (RunId)
    );
END
GO

GO

/* ---- 022_GraphSnapshotEdges_IndexKeyLength.sql ---- */
-- Replace IX_GraphSnapshotEdges_FromTo: (GraphSnapshotId + NVARCHAR(500) + NVARCHAR(500)) exceeds SQL Server's
-- 1700-byte nonclustered index key limit (2016 bytes). Use key (GraphSnapshotId, FromNodeId) with INCLUDE (ToNodeId, ...).
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = N'IX_GraphSnapshotEdges_FromTo'
      AND i.object_id = OBJECT_ID(N'dbo.GraphSnapshotEdges'))
BEGIN
    DROP INDEX IX_GraphSnapshotEdges_FromTo ON dbo.GraphSnapshotEdges;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = N'IX_GraphSnapshotEdges_SnapshotFrom'
      AND i.object_id = OBJECT_ID(N'dbo.GraphSnapshotEdges'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GraphSnapshotEdges_SnapshotFrom
        ON dbo.GraphSnapshotEdges (GraphSnapshotId, FromNodeId)
        INCLUDE (ToNodeId, EdgeType, Weight);
END;
GO

GO

/* ---- 023_ContextSnapshotRelationalChildren.sql ---- */
-- Relational child tables for dbo.ContextSnapshots (dual-write with legacy JSON). Mirrors ArchiForge.Persistence/Scripts/ArchiForge.sql.
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

/* ---- 024_GraphSnapshotRelationalChildren.sql ---- */
-- Relational children for dbo.GraphSnapshots (dual-write; JSON retained). Mirrors ArchiForge.Persistence/Scripts/ArchiForge.sql. GraphSnapshotEdges unchanged for ListIndexedEdgesAsync.
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

/* ---- 025_FindingsSnapshotRelational.sql ---- */
-- Relational findings + header SchemaVersion (dual-write with FindingsJson). Mirrors ArchiForge.Persistence/Scripts/ArchiForge.sql.
-- FindingsSnapshots is created here if missing (reference script ArchiForge.sql is not applied by DbUp).
IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingsSnapshots
    (
        FindingsSnapshotId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ContextSnapshotId UNIQUEIDENTIFIER NOT NULL,
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        SchemaVersion INT NOT NULL DEFAULT (1),
        FindingsJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_FindingsSnapshots_RunId ON dbo.FindingsSnapshots (RunId);
    CREATE NONCLUSTERED INDEX IX_FindingsSnapshots_ContextSnapshotId ON dbo.FindingsSnapshots (ContextSnapshotId);
    CREATE NONCLUSTERED INDEX IX_FindingsSnapshots_GraphSnapshotId ON dbo.FindingsSnapshots (GraphSnapshotId);
END;

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.FindingsSnapshots', N'SchemaVersion') IS NULL
BEGIN
    ALTER TABLE dbo.FindingsSnapshots
        ADD SchemaVersion INT NOT NULL CONSTRAINT DF_FindingsSnapshots_SchemaVersion_Brownfield DEFAULT (1);
END;

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_Runs_RunId')
BEGIN
    ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_ContextSnapshots_ContextSnapshotId')
BEGIN
    ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_ContextSnapshots_ContextSnapshotId
        FOREIGN KEY (ContextSnapshotId) REFERENCES dbo.ContextSnapshots (SnapshotId);
END;

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_GraphSnapshots_GraphSnapshotId')
BEGIN
    ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_GraphSnapshots_GraphSnapshotId
        FOREIGN KEY (GraphSnapshotId) REFERENCES dbo.GraphSnapshots (GraphSnapshotId);
END;

IF OBJECT_ID(N'dbo.FindingRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingRecords
    (
        FindingRecordId      UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_FindingRecords PRIMARY KEY,
        FindingsSnapshotId   UNIQUEIDENTIFIER NOT NULL,
        SortOrder            INT NOT NULL,
        FindingId            NVARCHAR(200) NOT NULL,
        FindingSchemaVersion INT NOT NULL,
        FindingType          NVARCHAR(200) NOT NULL,
        Category             NVARCHAR(200) NOT NULL,
        EngineType           NVARCHAR(200) NOT NULL,
        Severity             NVARCHAR(50) NOT NULL,
        Title                NVARCHAR(1000) NOT NULL,
        Rationale            NVARCHAR(MAX) NOT NULL,
        PayloadType          NVARCHAR(256) NULL,
        PayloadJson          NVARCHAR(MAX) NULL,
        CONSTRAINT FK_FindingRecords_FindingsSnapshots FOREIGN KEY (FindingsSnapshotId)
            REFERENCES dbo.FindingsSnapshots (FindingsSnapshotId) ON DELETE CASCADE,
        CONSTRAINT UQ_FindingRecords_Snapshot_Sort UNIQUE (FindingsSnapshotId, SortOrder)
    );

    CREATE NONCLUSTERED INDEX IX_FindingRecords_FindingsSnapshotId
        ON dbo.FindingRecords (FindingsSnapshotId);
END;

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

IF OBJECT_ID(N'dbo.FindingProperties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingProperties
    (
        FindingRecordId   UNIQUEIDENTIFIER NOT NULL,
        PropertySortOrder INT NOT NULL,
        PropertyKey       NVARCHAR(200) NOT NULL,
        PropertyValue     NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_FindingProperties PRIMARY KEY (FindingRecordId, PropertySortOrder),
        CONSTRAINT FK_FindingProperties_FindingRecords FOREIGN KEY (FindingRecordId)
            REFERENCES dbo.FindingRecords (FindingRecordId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FindingProperties_Record
        ON dbo.FindingProperties (FindingRecordId);
END;

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

/* ---- 026_GoldenManifestPhase1Relational.sql ---- */
-- Phase-1 GoldenManifest relational slices (dual-write with JSON columns). Mirrors ArchiForge.Persistence/Scripts/ArchiForge.sql.
-- DbUp runs embedded migrations for every SQL deployment; ArchiForge.sql (ISchemaBootstrapper) runs only when StorageProvider=Sql.
-- Ensure parent tables exist so this script succeeds when bootstrap is skipped (e.g. integration tests).
IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NULL
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

IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NULL
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
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        INDEX IX_GoldenManifests_RunId NONCLUSTERED (RunId)
    );
END;

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DecisioningTraces_Runs_RunId')
        ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT FK_DecisioningTraces_Runs_RunId
            FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;

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

IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_GoldenManifests_RunId'
          AND object_id = OBJECT_ID(N'dbo.GoldenManifests'))
        CREATE UNIQUE INDEX UX_GoldenManifests_RunId ON dbo.GoldenManifests (RunId);
END;

IF OBJECT_ID(N'dbo.GoldenManifestAssumptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestAssumptions
    (
        ManifestId      UNIQUEIDENTIFIER NOT NULL,
        SortOrder       INT NOT NULL,
        AssumptionText  NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_GoldenManifestAssumptions PRIMARY KEY (ManifestId, SortOrder),
        CONSTRAINT FK_GoldenManifestAssumptions_GoldenManifests FOREIGN KEY (ManifestId)
            REFERENCES dbo.GoldenManifests (ManifestId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GoldenManifestAssumptions_ManifestId
        ON dbo.GoldenManifestAssumptions (ManifestId);
END;

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

/* ---- 027_ArtifactBundleRelational.sql ---- */
-- Relational artifact bundle slices (dual-write with ArtifactsJson / TraceJson). Mirrors ArchiForge.Persistence/Scripts/ArchiForge.sql.
-- DbUp-only deployments may not run ISchemaBootstrapper; ensure dbo.ArtifactBundles exists before child tables.
IF OBJECT_ID(N'dbo.ArtifactBundles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundles
    (
        BundleId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ArtifactsJson NVARCHAR(MAX) NOT NULL,
        TraceJson NVARCHAR(MAX) NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        INDEX IX_ArtifactBundles_RunId NONCLUSTERED (RunId),
        INDEX IX_ArtifactBundles_ManifestId NONCLUSTERED (ManifestId)
    );
END;

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
        CONSTRAINT PK_ArtifactBundleArtifacts PRIMARY KEY (BundleId, SortOrder),
        CONSTRAINT UQ_ArtifactBundleArtifacts_ArtifactId UNIQUE (BundleId, ArtifactId),
        CONSTRAINT FK_ArtifactBundleArtifacts_Bundles FOREIGN KEY (BundleId)
            REFERENCES dbo.ArtifactBundles (BundleId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleArtifacts_BundleId
        ON dbo.ArtifactBundleArtifacts (BundleId);
END;

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

/* ---- 028_ArchivalSoftFlags.sql ---- */
-- Soft archival: hide aged runs, digests, and conversation threads from normal API lists/detail without deleting rows.
-- ConversationThreads may exist only after full ArchiForge.sql bootstrap; guard with OBJECT_ID for DbUp-only databases.
IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.Runs', 'ArchivedUtc') IS NULL
    ALTER TABLE dbo.Runs ADD ArchivedUtc DATETIME2 NULL;

IF OBJECT_ID(N'dbo.ArchitectureDigests', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.ArchitectureDigests', 'ArchivedUtc') IS NULL
    ALTER TABLE dbo.ArchitectureDigests ADD ArchivedUtc DATETIME2 NULL;

IF OBJECT_ID(N'dbo.ConversationThreads', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.ConversationThreads', 'ArchivedUtc') IS NULL
    ALTER TABLE dbo.ConversationThreads ADD ArchivedUtc DATETIME2 NULL;

GO

/* ---- 029_PolicyPackAssignments_ArchivedUtc.sql ---- */
-- Soft-delete / archival for governance assignments: archived rows stay for audit but are excluded from resolution lists.

IF OBJECT_ID(N'dbo.PolicyPackAssignments', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.PolicyPackAssignments', 'ArchivedUtc') IS NULL
    ALTER TABLE dbo.PolicyPackAssignments ADD ArchivedUtc DATETIME2 NULL;

GO

GO

/* ---- 030_RlsPilot_Runs.sql ---- */
-- Pilot row-level security on dbo.Runs (defense-in-depth). Policy ships with STATE = OFF; enable after app sets SESSION_CONTEXT (SqlServer:RowLevelSecurity:ApplySessionContext).
-- Trusted jobs (archival, schema bootstrap) set SESSION_CONTEXT key af_rls_bypass = 1 via the API layer.

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'rls')
    EXEC(N'CREATE SCHEMA rls');
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'RunsScopeFilter')
    DROP SECURITY POLICY rls.RunsScopeFilter;
GO

IF OBJECT_ID(N'rls.runs_scope_predicate', N'IF') IS NOT NULL
    DROP FUNCTION rls.runs_scope_predicate;
GO

CREATE FUNCTION rls.runs_scope_predicate(
    @TenantId uniqueidentifier,
    @WorkspaceId uniqueidentifier,
    @ScopeProjectId uniqueidentifier)
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
        AND @ScopeProjectId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'af_project_id'))
       )
);
GO

CREATE SECURITY POLICY rls.RunsScopeFilter
    ADD FILTER PREDICATE rls.runs_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.Runs
    WITH (STATE = OFF);
GO

GO

/* ---- 031_ProductLearningPilotSignals.sql ---- */
-- 58R â€” Pilot / product-learning signals: trusted vs rejected vs revised outputs, optional pattern keys for aggregation.
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
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningPilotSignals_ArchitectureRun')
    ALTER TABLE dbo.ProductLearningPilotSignals ADD CONSTRAINT FK_ProductLearningPilotSignals_ArchitectureRun
        FOREIGN KEY (ArchitectureRunId) REFERENCES dbo.ArchitectureRuns (RunId);
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

GO

/* ---- 032_ProductLearningPlanningBridge.sql ---- */
-- 59R â€” Learning-to-planning bridge: structured improvement themes, bounded plans, and explicit links to runs, signals, and artifacts.
-- Human-reviewable persistence only; no autonomous mutation of generation or evaluation logic.
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

IF OBJECT_ID(N'dbo.ProductLearningImprovementPlanArchitectureRuns', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningImprovementPlanArchitectureRuns_Run')
    ALTER TABLE dbo.ProductLearningImprovementPlanArchitectureRuns ADD CONSTRAINT FK_ProductLearningImprovementPlanArchitectureRuns_Run
        FOREIGN KEY (ArchitectureRunId) REFERENCES dbo.ArchitectureRuns (RunId);
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

GO

/* ---- 033_EvolutionSimulation.sql ---- */
-- 60R â€” Controlled evolution: candidate change sets from 59R improvement plans, shadow evaluation (read-only analysis only; no automatic system mutation).

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

IF OBJECT_ID(N'dbo.EvolutionSimulationRuns', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EvolutionSimulationRuns_ArchitectureRun')
    ALTER TABLE dbo.EvolutionSimulationRuns ADD CONSTRAINT FK_EvolutionSimulationRuns_ArchitectureRun
        FOREIGN KEY (BaselineArchitectureRunId) REFERENCES dbo.ArchitectureRuns (RunId);
GO

GO

/* ---- 034_LargeArtifactBlobPointers.sql ---- */
-- Large artifact pointers: optional blob URIs alongside inline NVARCHAR(MAX) (dual-write read prefers blob when present).

IF COL_LENGTH('dbo.GoldenManifests', 'ManifestPayloadBlobUri') IS NULL
    ALTER TABLE dbo.GoldenManifests ADD ManifestPayloadBlobUri NVARCHAR(2000) NULL;
GO

IF COL_LENGTH('dbo.ArtifactBundles', 'BundlePayloadBlobUri') IS NULL
    ALTER TABLE dbo.ArtifactBundles ADD BundlePayloadBlobUri NVARCHAR(2000) NULL;
GO

IF COL_LENGTH('dbo.ArtifactBundleArtifacts', 'ContentBlobUri') IS NULL
    ALTER TABLE dbo.ArtifactBundleArtifacts ADD ContentBlobUri NVARCHAR(2000) NULL;
GO

GO

/* ---- 035_AuditProvenanceConversationTables.sql ---- */
-- Tables required before 036_RlsArchiforgeTenantScope: those objects are created in Scripts/ArchiForge.sql
-- when StorageProvider=Sql, but DbUp-only databases (e.g. API integration tests with InMemory storage)
-- skip ISchemaBootstrapper and must still have these tables for CREATE SECURITY POLICY targets.

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NULL
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

IF OBJECT_ID(N'dbo.ProvenanceSnapshots', N'U') IS NULL
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

IF OBJECT_ID(N'dbo.ConversationThreads', N'U') IS NULL
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

GO

/* ---- 035_HostLeaderLeases.sql ---- */
/* Distributed leader leases for singleton hosted services (advisory scan, archival, retrieval outbox). */
IF OBJECT_ID(N'dbo.HostLeaderLeases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HostLeaderLeases
    (
        LeaseName          NVARCHAR(128) NOT NULL CONSTRAINT PK_HostLeaderLeases PRIMARY KEY,
        HolderInstanceId   NVARCHAR(256) NOT NULL,
        LeaseExpiresUtc    DATETIME2     NOT NULL
    );
END;
GO

GO

/* ---- 036_RlsArchiforgeTenantScope.sql ---- */
/*
  Row-level security: tenant / workspace / project isolation on all scope-keyed authority tables.

  Replaces pilot rls.RunsScopeFilter (dbo.Runs only) with rls.ArchiforgeTenantScope.

  SESSION_CONTEXT keys (set by RlsSessionContextApplicator when SqlServer:RowLevelSecurity:ApplySessionContext is true):
    af_rls_bypass, af_tenant_id, af_workspace_id, af_project_id

  Tables without TenantId/WorkspaceId/ProjectId on the row (e.g. ConversationMessages, ContextSnapshots child tables,
  PolicyPackVersions) are not covered â€” application layer must enforce; see docs/security/MULTI_TENANT_RLS.md.

  Ships WITH (STATE = OFF). After enabling ApplySessionContext in the app, turn policies on:
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope WITH (STATE = ON);
*/

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
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ArchitectureRunIdempotency,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningPilotSignals,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementThemes,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductLearningImprovementPlans,
    ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.EvolutionCandidateChangeSets
    WITH (STATE = OFF);
GO

GO

/* ---- 037_AuthorityPipelineWorkOutbox.sql ---- */
-- Deferred authority pipeline: context ingestion + graph (+ downstream stages) processed by worker after run header commits.
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
END
GO

GO

/* ---- 039_RowVersion_OptimisticConcurrency.sql ---- */
-- Adds ROWVERSION columns for optimistic concurrency on high-churn tables (Runs wired in app code; others reserved for future updates).

IF COL_LENGTH('dbo.Runs', 'RowVersionStamp') IS NULL
    ALTER TABLE dbo.Runs ADD RowVersionStamp ROWVERSION;

IF COL_LENGTH('dbo.GoldenManifests', 'RowVersionStamp') IS NULL
    ALTER TABLE dbo.GoldenManifests ADD RowVersionStamp ROWVERSION;

IF COL_LENGTH('dbo.PolicyPackAssignments', 'RowVersionStamp') IS NULL
    ALTER TABLE dbo.PolicyPackAssignments ADD RowVersionStamp ROWVERSION;

GO

/* ---- 040_IntegrationEventOutbox.sql ---- */
-- Transactional outbox for integration events (e.g. authority run completed â†’ Service Bus).
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
        ProcessedUtc DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX IX_IntegrationEventOutbox_Pending
        ON dbo.IntegrationEventOutbox (ProcessedUtc, CreatedUtc)
        WHERE ProcessedUtc IS NULL;
END;
GO

-- RLS: add predicate to existing tenant scope policy when present (idempotent for re-runs).
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'IntegrationEventOutbox')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.IntegrationEventOutbox;
END;
GO

GO

/* ---- 041_IntegrationEventOutbox_RetryDeadLetter.sql ---- */
-- Retry / backoff and dead-letter columns for integration event outbox (Service Bus publish failures).

IF COL_LENGTH('dbo.IntegrationEventOutbox', 'RetryCount') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationEventOutbox ADD
        RetryCount INT NOT NULL CONSTRAINT DF_IntegrationEventOutbox_RetryCount DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.IntegrationEventOutbox', 'NextRetryUtc') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationEventOutbox ADD NextRetryUtc DATETIME2 NULL;
END;
GO

IF COL_LENGTH('dbo.IntegrationEventOutbox', 'LastErrorMessage') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationEventOutbox ADD LastErrorMessage NVARCHAR(2048) NULL;
END;
GO

IF COL_LENGTH('dbo.IntegrationEventOutbox', 'DeadLetteredUtc') IS NULL
BEGIN
    ALTER TABLE dbo.IntegrationEventOutbox ADD DeadLetteredUtc DATETIME2 NULL;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_IntegrationEventOutbox_Pending'
      AND object_id = OBJECT_ID(N'dbo.IntegrationEventOutbox'))
BEGIN
    DROP INDEX IX_IntegrationEventOutbox_Pending ON dbo.IntegrationEventOutbox;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_IntegrationEventOutbox_Pending'
      AND object_id = OBJECT_ID(N'dbo.IntegrationEventOutbox'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_IntegrationEventOutbox_Pending
        ON dbo.IntegrationEventOutbox (ProcessedUtc, NextRetryUtc, CreatedUtc)
        WHERE ProcessedUtc IS NULL AND DeadLetteredUtc IS NULL;
END;
GO

GO

/* ---- 042_GraphSnapshots_LegacyJsonNullable.sql ---- */
-- Legacy dual-write JSON columns on GraphSnapshots may be unset when a header row is inserted before JSON backfill.
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

/* ---- 043_ArtifactBundles_LegacyJsonNullable.sql ---- */
-- Legacy dual-write JSON columns on ArtifactBundles may be unset when relational slices
-- are authoritative or a header row is inserted before JSON backfill (see SqlArtifactBundleRepository).
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

/* ---- 044_DdlScriptRenameReference.sql ---- */
-- Reference-only migration: master consolidated DDL script was renamed ArchiForge.sql â†’ ArchLucid.sql (Phase 7 rename).
-- No schema changes. DbUp records this script in the journal for continuity.

GO

/* ---- 045_GraphSnapshotEdgeLabelKeyArchLucid.sql ---- */
-- Rename stored edge label property key (Phase 7 product rename).
-- Also correct any rows migrated with the mistaken '$ArchiLucid:EdgeLabel' spelling.
UPDATE dbo.GraphSnapshotEdgeProperties
SET PropertyKey = N'$ArchLucid:EdgeLabel'
WHERE PropertyKey IN (N'$ArchiForge:EdgeLabel', N'$ArchiLucid:EdgeLabel');

GO

/* ---- 046_RlsDenormalizeChildTables.sql ---- */
/*
  RLS defense-in-depth: denormalize tenant/workspace/project scope onto high-traffic child tables
  dbo.ContextSnapshots, dbo.FindingsSnapshots, dbo.GoldenManifestAssumptions.

  ContextSnapshots already has ProjectId NVARCHAR(200) for logical project key; RLS uses
  ScopeProjectId UNIQUEIDENTIFIER (aligned with dbo.Runs.ScopeProjectId / SESSION_CONTEXT af_project_id).

  Adds FILTER predicates to rls.ArchiforgeTenantScope idempotently (skip if already present).

  See docs/security/MULTI_TENANT_RLS.md.
*/

SET XACT_ABORT ON;
GO

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

UPDATE cs
SET
    cs.TenantId = r.TenantId,
    cs.WorkspaceId = r.WorkspaceId,
    cs.ScopeProjectId = r.ScopeProjectId
FROM dbo.ContextSnapshots AS cs
INNER JOIN dbo.Runs AS r ON cs.RunId = r.RunId
WHERE cs.TenantId IS NULL;
GO

UPDATE fs
SET
    fs.TenantId = r.TenantId,
    fs.WorkspaceId = r.WorkspaceId,
    fs.ProjectId = r.ScopeProjectId
FROM dbo.FindingsSnapshots AS fs
INNER JOIN dbo.Runs AS r ON fs.RunId = r.RunId
WHERE fs.TenantId IS NULL;
GO

UPDATE gma
SET
    gma.TenantId = gm.TenantId,
    gma.WorkspaceId = gm.WorkspaceId,
    gma.ProjectId = gm.ProjectId
FROM dbo.GoldenManifestAssumptions AS gma
INNER JOIN dbo.GoldenManifests AS gm ON gma.ManifestId = gm.ManifestId
WHERE gma.TenantId IS NULL;
GO

DECLARE @PolicyObjectId INT;

SELECT @PolicyObjectId = object_id
FROM sys.security_policies
WHERE name = N'ArchiforgeTenantScope';

IF @PolicyObjectId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS sp
        WHERE sp.object_id = @PolicyObjectId
          AND sp.target_object_id = OBJECT_ID(N'dbo.ContextSnapshots', N'U'))
        EXEC(N'ALTER SECURITY POLICY rls.ArchiforgeTenantScope ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.ContextSnapshots');

    IF NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS sp
        WHERE sp.object_id = @PolicyObjectId
          AND sp.target_object_id = OBJECT_ID(N'dbo.FindingsSnapshots', N'U'))
        EXEC(N'ALTER SECURITY POLICY rls.ArchiforgeTenantScope ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingsSnapshots');

    IF NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS sp
        WHERE sp.object_id = @PolicyObjectId
          AND sp.target_object_id = OBJECT_ID(N'dbo.GoldenManifestAssumptions', N'U'))
        EXEC(N'ALTER SECURITY POLICY rls.ArchiforgeTenantScope ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GoldenManifestAssumptions');
END;
GO

GO

/* ---- 047_DropForeignKeysToArchitectureRuns.sql ---- */
/*
  Migration 047: Drop FK constraints from coordinator / learning tables to dbo.ArchitectureRuns.

  Explicit constraint inventory (all reference dbo.ArchitectureRuns (RunId)):
  1. FK_AgentTasks_Run
  2. FK_AgentResults_Run
  3. FK_GoldenManifestVersions_Run
  4. FK_DecisionTraces_Run
  5. FK_AgentEvidencePackages_Run
  6. FK_AgentExecutionTraces_Run
  7. FK_RunExportRecords_Run
  8. FK_ComparisonRecords_LeftRun
  9. FK_ComparisonRecords_RightRun
  10. FK_DecisionNodes_Run
  11. FK_AgentEvaluations_Run
  12. FK_ArchitectureRunIdempotency_Run
  13. FK_ProductLearningPilotSignals_ArchitectureRun
  14. FK_ProductLearningImprovementPlanArchitectureRuns_Run
  15. FK_EvolutionSimulationRuns_ArchitectureRun

  Rationale (ADR-0012): NVARCHAR(64) string RunId columns cannot reference dbo.Runs.RunId
  (UNIQUEIDENTIFIER) without type migration. No replacement FK to dbo.Runs in this migration;
  integrity is application-enforced alongside dbo.Runs authority rows.

  Idempotent: each DROP is guarded by sys.foreign_keys.
*/

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentTasks_Run')
    ALTER TABLE dbo.AgentTasks DROP CONSTRAINT FK_AgentTasks_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentResults_Run')
    ALTER TABLE dbo.AgentResults DROP CONSTRAINT FK_AgentResults_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifestVersions_Run')
    ALTER TABLE dbo.GoldenManifestVersions DROP CONSTRAINT FK_GoldenManifestVersions_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DecisionTraces_Run')
    ALTER TABLE dbo.DecisionTraces DROP CONSTRAINT FK_DecisionTraces_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvidencePackages_Run')
    ALTER TABLE dbo.AgentEvidencePackages DROP CONSTRAINT FK_AgentEvidencePackages_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentExecutionTraces_Run')
    ALTER TABLE dbo.AgentExecutionTraces DROP CONSTRAINT FK_AgentExecutionTraces_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RunExportRecords_Run')
    ALTER TABLE dbo.RunExportRecords DROP CONSTRAINT FK_RunExportRecords_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ComparisonRecords_LeftRun')
    ALTER TABLE dbo.ComparisonRecords DROP CONSTRAINT FK_ComparisonRecords_LeftRun;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ComparisonRecords_RightRun')
    ALTER TABLE dbo.ComparisonRecords DROP CONSTRAINT FK_ComparisonRecords_RightRun;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DecisionNodes_Run')
    ALTER TABLE dbo.DecisionNodes DROP CONSTRAINT FK_DecisionNodes_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentEvaluations_Run')
    ALTER TABLE dbo.AgentEvaluations DROP CONSTRAINT FK_AgentEvaluations_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArchitectureRunIdempotency_Run')
    ALTER TABLE dbo.ArchitectureRunIdempotency DROP CONSTRAINT FK_ArchitectureRunIdempotency_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningPilotSignals_ArchitectureRun')
    ALTER TABLE dbo.ProductLearningPilotSignals DROP CONSTRAINT FK_ProductLearningPilotSignals_ArchitectureRun;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductLearningImprovementPlanArchitectureRuns_Run')
    ALTER TABLE dbo.ProductLearningImprovementPlanArchitectureRuns DROP CONSTRAINT FK_ProductLearningImprovementPlanArchitectureRuns_Run;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EvolutionSimulationRuns_ArchitectureRun')
    ALTER TABLE dbo.EvolutionSimulationRuns DROP CONSTRAINT FK_EvolutionSimulationRuns_ArchitectureRun;
GO

GO

/* ---- 048_RunsLegacyReadAlignment.sql ---- */
/* DbUp 048: dbo.Runs columns for converging legacy ArchitectureRuns read surface (ADR-0012 read path). */

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
GO

/* Backfill from dbo.ArchitectureRuns where a matching legacy row exists (RunId = no-dash lower hex). */
IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ArchitectureRuns', N'U') IS NOT NULL
BEGIN
    UPDATE r
    SET
        ArchitectureRequestId = COALESCE(r.ArchitectureRequestId, ar.RequestId),
        LegacyRunStatus = COALESCE(r.LegacyRunStatus, ar.Status),
        CompletedUtc = COALESCE(r.CompletedUtc, ar.CompletedUtc),
        CurrentManifestVersion = COALESCE(r.CurrentManifestVersion, ar.CurrentManifestVersion)
    FROM dbo.Runs r
    INNER JOIN dbo.ArchitectureRuns ar
        ON ar.RunId = LOWER(REPLACE(CONVERT(NCHAR(36), r.RunId), N'-', N''));
END;
GO

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

GO

/* ---- 049_DropArchitectureRunsTable.sql ---- */
/*
  Migration 049: Drop legacy dbo.ArchitectureRuns.

  Authority run state lives in dbo.Runs (UNIQUEIDENTIFIER RunId). Migration 047 removed inbound FKs
  from coordinator / learning tables to ArchitectureRuns; this migration retires the table.

  Idempotent: DROP only when the table exists.
*/

IF OBJECT_ID(N'dbo.ArchitectureRuns', N'U') IS NOT NULL
    DROP TABLE dbo.ArchitectureRuns;
GO

GO

/* ---- 050_PolicyPackChangeLog.sql ---- */
/*
  Migration 050: Append-only policy pack change log.

  Records every mutation to policy packs, versions, and assignments.
  The application identity should have INSERT-only permissions on this
  table; UPDATE and DELETE are prohibited by design.

  Idempotent: skips creation when the table exists.
*/

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

-- RLS: add predicate when tenant scope policy exists (idempotent for re-runs).
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'PolicyPackChangeLog')
BEGIN
    ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog;
END;
GO

GO

