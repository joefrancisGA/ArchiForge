-- 060: Indexes for query patterns identified as under-indexed

-- AuditEvents: EventType filter used by /v1/audit/search?eventType=...
-- Existing IX_AuditEvents_Scope_OccurredUtc leads with scope columns but EventType
-- is appended as a residual predicate, causing extra page reads on large audit tables.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_AuditEvents_EventType_OccurredUtc'
      AND object_id = OBJECT_ID(N'dbo.AuditEvents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditEvents_EventType_OccurredUtc
        ON dbo.AuditEvents (TenantId, WorkspaceId, ProjectId, EventType, OccurredUtc DESC);
END

-- ConversationThreads: all list/count queries filter ArchivedUtc IS NULL
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ConversationThreads_Scope_Active'
      AND object_id = OBJECT_ID(N'dbo.ConversationThreads'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ConversationThreads_Scope_Active
        ON dbo.ConversationThreads (TenantId, WorkspaceId, ProjectId, LastUpdatedUtc DESC)
        WHERE ArchivedUtc IS NULL;
END

-- GovernanceEnvironmentActivations: GetByRunIdAsync uses WHERE RunId = @RunId ORDER BY ActivatedUtc DESC
-- Migration 017 only created IX_..._Environment_IsActive — no RunId index.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceEnvironmentActivations_RunId_ActivatedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceEnvironmentActivations_RunId_ActivatedUtc
        ON dbo.GovernanceEnvironmentActivations (RunId, ActivatedUtc DESC);
END

-- GovernanceEnvironmentActivations: GetByEnvironmentAsync uses WHERE Environment = @Environment ORDER BY ActivatedUtc DESC
-- Existing index (Environment, IsActive) does not cover the ActivatedUtc sort.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceEnvironmentActivations_Environment_ActivatedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceEnvironmentActivations_Environment_ActivatedUtc
        ON dbo.GovernanceEnvironmentActivations (Environment, ActivatedUtc DESC)
        INCLUDE (RunId, ManifestVersion, IsActive);
END

-- GovernancePromotionRecords: GetByRunIdAsync uses WHERE RunId ORDER BY PromotedUtc DESC
-- Migration 017 only created IX_..._RunId with no sort column.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernancePromotionRecords_RunId_PromotedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernancePromotionRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernancePromotionRecords_RunId_PromotedUtc
        ON dbo.GovernancePromotionRecords (RunId, PromotedUtc DESC);
END

-- RecommendationRecords: ListByRunAsync uses ORDER BY PriorityScore DESC, CreatedUtc DESC
-- IX_RecommendationRecords_Scope_Run leads with scope + RunId + CreatedUtc but lacks PriorityScore.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_RecommendationRecords_Scope_Run_Priority'
      AND object_id = OBJECT_ID(N'dbo.RecommendationRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RecommendationRecords_Scope_Run_Priority
        ON dbo.RecommendationRecords (TenantId, WorkspaceId, ProjectId, RunId, PriorityScore DESC, CreatedUtc DESC);
END

-- RecommendationRecords: ListByScopeAsync with @Status IS NULL uses ORDER BY LastUpdatedUtc DESC
-- IX_RecommendationRecords_Scope_Status leads with Status, which does not help the unfiltered case.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_RecommendationRecords_Scope_LastUpdatedUtc'
      AND object_id = OBJECT_ID(N'dbo.RecommendationRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RecommendationRecords_Scope_LastUpdatedUtc
        ON dbo.RecommendationRecords (TenantId, WorkspaceId, ProjectId, LastUpdatedUtc DESC);
END

-- Runs: global archive retention job uses WHERE ArchivedUtc IS NULL AND CreatedUtc < @Cutoff
-- with no scope columns. IX_Runs_Scope_CreatedUtc is filtered but leads with TenantId.
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

-- PolicyPackAssignments: ListByScopeAsync filters ArchivedUtc IS NULL with OR-based ScopeLevel checks.
-- Existing indexes do not include ArchivedUtc filter.
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
