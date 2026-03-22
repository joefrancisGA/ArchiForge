/*
  Authority-chain store (Dapper). Table DecisioningTraces is used instead of DecisionTraces
  because dbo.DecisionTraces already exists for the API/commit trail (ArchiForge.Data).
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
        ArtifactBundleId UNIQUEIDENTIFIER NULL
    );

    CREATE INDEX IX_Runs_ProjectId_CreatedUtc
        ON dbo.Runs(ProjectId, CreatedUtc DESC);
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
        SourceHashesJson NVARCHAR(MAX) NOT NULL
    );

    CREATE INDEX IX_ContextSnapshots_ProjectId_CreatedUtc
        ON dbo.ContextSnapshots(ProjectId, CreatedUtc DESC);

    CREATE INDEX IX_ContextSnapshots_RunId
        ON dbo.ContextSnapshots(RunId);
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
        WarningsJson NVARCHAR(MAX) NOT NULL
    );

    CREATE INDEX IX_GraphSnapshots_RunId
        ON dbo.GraphSnapshots(RunId);

    CREATE INDEX IX_GraphSnapshots_ContextSnapshotId
        ON dbo.GraphSnapshots(ContextSnapshotId);
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
        FindingsJson NVARCHAR(MAX) NOT NULL
    );

    CREATE INDEX IX_FindingsSnapshots_RunId
        ON dbo.FindingsSnapshots(RunId);

    CREATE INDEX IX_FindingsSnapshots_ContextSnapshotId
        ON dbo.FindingsSnapshots(ContextSnapshotId);

    CREATE INDEX IX_FindingsSnapshots_GraphSnapshotId
        ON dbo.FindingsSnapshots(GraphSnapshotId);
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
        NotesJson NVARCHAR(MAX) NOT NULL
    );

    CREATE INDEX IX_DecisioningTraces_RunId
        ON dbo.DecisioningTraces(RunId);
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
        ProvenanceJson NVARCHAR(MAX) NOT NULL
    );

    CREATE INDEX IX_GoldenManifests_RunId
        ON dbo.GoldenManifests(RunId);
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
        TraceJson NVARCHAR(MAX) NOT NULL
    );

    CREATE INDEX IX_ArtifactBundles_RunId
        ON dbo.ArtifactBundles(RunId);

    CREATE INDEX IX_ArtifactBundles_ManifestId
        ON dbo.ArtifactBundles(ManifestId);
END;
GO

IF COL_LENGTH('dbo.GoldenManifests', 'ComplianceJson') IS NULL
BEGIN
    ALTER TABLE dbo.GoldenManifests
        ADD ComplianceJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_GoldenManifests_ComplianceJson DEFAULT (N'{}');
END;
GO

/* --- Multi-tenant scope (Tenant / Workspace / Project GUID) --- */

IF COL_LENGTH('dbo.Runs', 'TenantId') IS NULL
BEGIN
    ALTER TABLE dbo.Runs ADD TenantId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Runs_TenantId DEFAULT ('11111111-1111-1111-1111-111111111111');
END;
GO

IF COL_LENGTH('dbo.Runs', 'WorkspaceId') IS NULL
BEGIN
    ALTER TABLE dbo.Runs ADD WorkspaceId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Runs_WorkspaceId DEFAULT ('22222222-2222-2222-2222-222222222222');
END;
GO

IF COL_LENGTH('dbo.Runs', 'ScopeProjectId') IS NULL
BEGIN
    ALTER TABLE dbo.Runs ADD ScopeProjectId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Runs_ScopeProjectId DEFAULT ('33333333-3333-3333-3333-333333333333');
END;
GO

IF COL_LENGTH('dbo.DecisioningTraces', 'TenantId') IS NULL
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD TenantId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_DecisioningTraces_TenantId DEFAULT ('11111111-1111-1111-1111-111111111111');
END;
GO

IF COL_LENGTH('dbo.DecisioningTraces', 'WorkspaceId') IS NULL
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD WorkspaceId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_DecisioningTraces_WorkspaceId DEFAULT ('22222222-2222-2222-2222-222222222222');
END;
GO

IF COL_LENGTH('dbo.DecisioningTraces', 'ProjectId') IS NULL
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD ProjectId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_DecisioningTraces_ProjectId DEFAULT ('33333333-3333-3333-3333-333333333333');
END;
GO

IF COL_LENGTH('dbo.GoldenManifests', 'TenantId') IS NULL
BEGIN
    ALTER TABLE dbo.GoldenManifests ADD TenantId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_GoldenManifests_TenantId DEFAULT ('11111111-1111-1111-1111-111111111111');
END;
GO

IF COL_LENGTH('dbo.GoldenManifests', 'WorkspaceId') IS NULL
BEGIN
    ALTER TABLE dbo.GoldenManifests ADD WorkspaceId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_GoldenManifests_WorkspaceId DEFAULT ('22222222-2222-2222-2222-222222222222');
END;
GO

IF COL_LENGTH('dbo.GoldenManifests', 'ProjectId') IS NULL
BEGIN
    ALTER TABLE dbo.GoldenManifests ADD ProjectId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_GoldenManifests_ProjectId DEFAULT ('33333333-3333-3333-3333-333333333333');
END;
GO

IF COL_LENGTH('dbo.ArtifactBundles', 'TenantId') IS NULL
BEGIN
    ALTER TABLE dbo.ArtifactBundles ADD TenantId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_ArtifactBundles_TenantId DEFAULT ('11111111-1111-1111-1111-111111111111');
END;
GO

IF COL_LENGTH('dbo.ArtifactBundles', 'WorkspaceId') IS NULL
BEGIN
    ALTER TABLE dbo.ArtifactBundles ADD WorkspaceId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_ArtifactBundles_WorkspaceId DEFAULT ('22222222-2222-2222-2222-222222222222');
END;
GO

IF COL_LENGTH('dbo.ArtifactBundles', 'ProjectId') IS NULL
BEGIN
    ALTER TABLE dbo.ArtifactBundles ADD ProjectId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_ArtifactBundles_ProjectId DEFAULT ('33333333-3333-3333-3333-333333333333');
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
        CorrelationId NVARCHAR(200) NULL
    );

    CREATE NONCLUSTERED INDEX IX_AuditEvents_Scope_OccurredUtc
        ON dbo.AuditEvents (TenantId, WorkspaceId, ProjectId, OccurredUtc DESC);
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
        CreatedUtc DATETIME2 NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_ProvenanceSnapshots_Scope_Run
        ON dbo.ProvenanceSnapshots (TenantId, WorkspaceId, ProjectId, RunId, CreatedUtc DESC);
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
        LastUpdatedUtc DATETIME2 NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_ConversationThreads_Scope
        ON dbo.ConversationThreads (TenantId, WorkspaceId, ProjectId, LastUpdatedUtc DESC);
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
        MetadataJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_ConversationMessages_ThreadId_CreatedUtc
        ON dbo.ConversationMessages (ThreadId, CreatedUtc ASC);
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
        SupportingArtifactIdsJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_RecommendationRecords_Scope_Run
        ON dbo.RecommendationRecords (TenantId, WorkspaceId, ProjectId, RunId, CreatedUtc DESC);

    CREATE NONCLUSTERED INDEX IX_RecommendationRecords_Scope_Status
        ON dbo.RecommendationRecords (TenantId, WorkspaceId, ProjectId, Status, LastUpdatedUtc DESC);
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
        ProfileJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_RecommendationLearningProfiles_Scope_GeneratedUtc
        ON dbo.RecommendationLearningProfiles (TenantId, WorkspaceId, ProjectId, GeneratedUtc DESC);
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
        NextRunUtc DATETIME2 NULL
    );

    CREATE NONCLUSTERED INDEX IX_AdvisoryScanSchedules_Scope_Enabled_NextRun
        ON dbo.AdvisoryScanSchedules (TenantId, WorkspaceId, ProjectId, IsEnabled, NextRunUtc);
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
        ErrorMessage NVARCHAR(MAX) NULL
    );

    CREATE NONCLUSTERED INDEX IX_AdvisoryScanExecutions_Schedule_StartedUtc
        ON dbo.AdvisoryScanExecutions (ScheduleId, StartedUtc DESC);
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
        MetadataJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_ArchitectureDigests_Scope_GeneratedUtc
        ON dbo.ArchitectureDigests (TenantId, WorkspaceId, ProjectId, GeneratedUtc DESC);
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
        MetadataJson NVARCHAR(MAX) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_DigestSubscriptions_Scope_Enabled
        ON dbo.DigestSubscriptions (TenantId, WorkspaceId, ProjectId, IsEnabled, CreatedUtc DESC);
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
        Destination NVARCHAR(1000) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_DigestDeliveryAttempts_DigestId_AttemptedUtc
        ON dbo.DigestDeliveryAttempts (DigestId, AttemptedUtc DESC);

    CREATE NONCLUSTERED INDEX IX_DigestDeliveryAttempts_SubscriptionId_AttemptedUtc
        ON dbo.DigestDeliveryAttempts (SubscriptionId, AttemptedUtc DESC);
END;
GO
