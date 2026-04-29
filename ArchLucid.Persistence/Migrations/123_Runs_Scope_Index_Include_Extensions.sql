/*
  Extend IX_Runs_Scope_CreatedUtc INCLUDE list with pilot/showcase columns used by list projections
  (migration 061 predates these columns).

  DbUp: runs after greenfield dbo.Runs DDL (ArchLucid.sql) and previous index migrations (061,085).
*/

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Runs_Scope_CreatedUtc'
      AND object_id = OBJECT_ID(N'dbo.Runs'))
BEGIN
    DROP INDEX IX_Runs_Scope_CreatedUtc ON dbo.Runs;
END;
GO

CREATE NONCLUSTERED INDEX IX_Runs_Scope_CreatedUtc
    ON dbo.Runs (TenantId, WorkspaceId, ScopeProjectId, CreatedUtc DESC)
    INCLUDE (
        RunId,
        ProjectId,
        Description,
        ContextSnapshotId,
        GraphSnapshotId,
        FindingsSnapshotId,
        GoldenManifestId,
        DecisionTraceId,
        ArtifactBundleId,
        ArchitectureRequestId,
        LegacyRunStatus,
        CompletedUtc,
        CurrentManifestVersion,
        OtelTraceId,
        IsPublicShowcase,
        RealModeFellBackToSimulator,
        PilotAoaiDeploymentSnapshot)
    WHERE ArchivedUtc IS NULL;
GO
