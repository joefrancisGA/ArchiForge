/* Roll back migration 123: restore INCLUDE set without newer columns (parity with DbUp 061). */

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
        ArchivedUtc)
    WHERE ArchivedUtc IS NULL;
GO
