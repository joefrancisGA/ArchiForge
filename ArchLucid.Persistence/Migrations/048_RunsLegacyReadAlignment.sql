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
