/*
  141_00: Ensure triple-scope columns exist before 141_TenantHealthScores_BatchRefresh.

  sp_TenantHealthScores_BatchRefresh joins dbo.GovernanceApprovalRequests on WorkspaceId and MERGEs
  dbo.TenantHealthScores with WorkspaceId/ProjectId. Legacy catalogs can have:
  - dbo.TenantHealthScores created with TenantId-only aggregate shape, or
  - governance tables with TenantId populated but WorkspaceId/ProjectId never added (118 only added
    all three when TenantId was absent).

  This script is idempotent and ordered lexicographically before 141_TenantHealthScores_BatchRefresh.sql.
*/

/* ---- dbo.TenantHealthScores (083 forward shape) ---- */
IF OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TenantHealthScores', N'WorkspaceId') IS NULL
    ALTER TABLE dbo.TenantHealthScores ADD WorkspaceId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TenantHealthScores', N'ProjectId') IS NULL
    ALTER TABLE dbo.TenantHealthScores ADD ProjectId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TenantHealthScores', N'WorkspaceId') IS NOT NULL
   AND COL_LENGTH(N'dbo.TenantHealthScores', N'ProjectId') IS NOT NULL
   AND OBJECT_ID(N'dbo.TenantWorkspaces', N'U') IS NOT NULL
BEGIN
    UPDATE ths
    SET
        ths.WorkspaceId = pw.WorkspaceId,
        ths.ProjectId = pw.ProjectId
    FROM dbo.TenantHealthScores AS ths
    INNER JOIN (
        SELECT
            tw.TenantId,
            tw.Id AS WorkspaceId,
            tw.DefaultProjectId AS ProjectId,
            ROW_NUMBER() OVER (PARTITION BY tw.TenantId ORDER BY tw.CreatedUtc ASC) AS Rn
        FROM dbo.TenantWorkspaces AS tw) AS pw
        ON pw.TenantId = ths.TenantId
       AND pw.Rn = 1
    WHERE ths.WorkspaceId IS NULL
       OR ths.ProjectId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TenantHealthScores', N'WorkspaceId') IS NOT NULL
   AND COL_LENGTH(N'dbo.TenantHealthScores', N'ProjectId') IS NOT NULL
BEGIN
    DELETE FROM dbo.TenantHealthScores
    WHERE TenantId IS NULL
       OR WorkspaceId IS NULL
       OR ProjectId IS NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.TenantHealthScores')
          AND c.name = N'WorkspaceId'
          AND c.is_nullable = 1)
        AND NOT EXISTS (
            SELECT 1
            FROM dbo.TenantHealthScores AS x
            WHERE x.WorkspaceId IS NULL)

        ALTER TABLE dbo.TenantHealthScores ALTER COLUMN WorkspaceId UNIQUEIDENTIFIER NOT NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.TenantHealthScores')
          AND c.name = N'ProjectId'
          AND c.is_nullable = 1)
        AND NOT EXISTS (
            SELECT 1
            FROM dbo.TenantHealthScores AS x
            WHERE x.ProjectId IS NULL)

        ALTER TABLE dbo.TenantHealthScores ALTER COLUMN ProjectId UNIQUEIDENTIFIER NOT NULL;
END;
GO

/* ---- Governance workflow tables (118 forward shape) ---- */
IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceApprovalRequests', N'TenantId') IS NULL
    ALTER TABLE dbo.GovernanceApprovalRequests ADD TenantId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceApprovalRequests', N'WorkspaceId') IS NULL
    ALTER TABLE dbo.GovernanceApprovalRequests ADD WorkspaceId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceApprovalRequests', N'ProjectId') IS NULL
    ALTER TABLE dbo.GovernanceApprovalRequests ADD ProjectId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.GovernancePromotionRecords', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernancePromotionRecords', N'TenantId') IS NULL
    ALTER TABLE dbo.GovernancePromotionRecords ADD TenantId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.GovernancePromotionRecords', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernancePromotionRecords', N'WorkspaceId') IS NULL
    ALTER TABLE dbo.GovernancePromotionRecords ADD WorkspaceId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.GovernancePromotionRecords', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernancePromotionRecords', N'ProjectId') IS NULL
    ALTER TABLE dbo.GovernancePromotionRecords ADD ProjectId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceEnvironmentActivations', N'TenantId') IS NULL
    ALTER TABLE dbo.GovernanceEnvironmentActivations ADD TenantId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceEnvironmentActivations', N'WorkspaceId') IS NULL
    ALTER TABLE dbo.GovernanceEnvironmentActivations ADD WorkspaceId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceEnvironmentActivations', N'ProjectId') IS NULL
    ALTER TABLE dbo.GovernanceEnvironmentActivations ADD ProjectId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
BEGIN
    UPDATE g
    SET
        g.TenantId = r.TenantId,
        g.WorkspaceId = r.WorkspaceId,
        g.ProjectId = r.ScopeProjectId
    FROM dbo.GovernanceApprovalRequests AS g
    INNER JOIN dbo.Runs AS r ON TRY_CONVERT(UNIQUEIDENTIFIER, g.RunId) = r.RunId
    WHERE g.TenantId IS NULL
       OR g.WorkspaceId IS NULL
       OR g.ProjectId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GovernancePromotionRecords', N'U') IS NOT NULL
BEGIN
    UPDATE g
    SET
        g.TenantId = r.TenantId,
        g.WorkspaceId = r.WorkspaceId,
        g.ProjectId = r.ScopeProjectId
    FROM dbo.GovernancePromotionRecords AS g
    INNER JOIN dbo.Runs AS r ON TRY_CONVERT(UNIQUEIDENTIFIER, g.RunId) = r.RunId
    WHERE g.TenantId IS NULL
       OR g.WorkspaceId IS NULL
       OR g.ProjectId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
BEGIN
    UPDATE g
    SET
        g.TenantId = r.TenantId,
        g.WorkspaceId = r.WorkspaceId,
        g.ProjectId = r.ScopeProjectId
    FROM dbo.GovernanceEnvironmentActivations AS g
    INNER JOIN dbo.Runs AS r ON TRY_CONVERT(UNIQUEIDENTIFIER, g.RunId) = r.RunId
    WHERE g.TenantId IS NULL
       OR g.WorkspaceId IS NULL
       OR g.ProjectId IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
    DELETE FROM dbo.GovernanceApprovalRequests
    WHERE TenantId IS NULL
       OR WorkspaceId IS NULL
       OR ProjectId IS NULL;
GO

IF OBJECT_ID(N'dbo.GovernancePromotionRecords', N'U') IS NOT NULL
    DELETE FROM dbo.GovernancePromotionRecords
    WHERE TenantId IS NULL
       OR WorkspaceId IS NULL
       OR ProjectId IS NULL;
GO

IF OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
    DELETE FROM dbo.GovernanceEnvironmentActivations
    WHERE TenantId IS NULL
       OR WorkspaceId IS NULL
       OR ProjectId IS NULL;
GO

IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.GovernanceApprovalRequests AS x
        WHERE x.TenantId IS NULL
           OR x.WorkspaceId IS NULL
           OR x.ProjectId IS NULL)
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests')
          AND c.name = N'TenantId'
          AND c.is_nullable = 1)

        ALTER TABLE dbo.GovernanceApprovalRequests ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests')
          AND c.name = N'WorkspaceId'
          AND c.is_nullable = 1)

        ALTER TABLE dbo.GovernanceApprovalRequests ALTER COLUMN WorkspaceId UNIQUEIDENTIFIER NOT NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests')
          AND c.name = N'ProjectId'
          AND c.is_nullable = 1)

        ALTER TABLE dbo.GovernanceApprovalRequests ALTER COLUMN ProjectId UNIQUEIDENTIFIER NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.GovernancePromotionRecords', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.GovernancePromotionRecords AS x
        WHERE x.TenantId IS NULL
           OR x.WorkspaceId IS NULL
           OR x.ProjectId IS NULL)
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernancePromotionRecords')
          AND c.name = N'TenantId'
          AND c.is_nullable = 1)

        ALTER TABLE dbo.GovernancePromotionRecords ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernancePromotionRecords')
          AND c.name = N'WorkspaceId'
          AND c.is_nullable = 1)

        ALTER TABLE dbo.GovernancePromotionRecords ALTER COLUMN WorkspaceId UNIQUEIDENTIFIER NOT NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernancePromotionRecords')
          AND c.name = N'ProjectId'
          AND c.is_nullable = 1)

        ALTER TABLE dbo.GovernancePromotionRecords ALTER COLUMN ProjectId UNIQUEIDENTIFIER NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.GovernanceEnvironmentActivations AS x
        WHERE x.TenantId IS NULL
           OR x.WorkspaceId IS NULL
           OR x.ProjectId IS NULL)
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations')
          AND c.name = N'TenantId'
          AND c.is_nullable = 1)

        ALTER TABLE dbo.GovernanceEnvironmentActivations ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations')
          AND c.name = N'WorkspaceId'
          AND c.is_nullable = 1)

        ALTER TABLE dbo.GovernanceEnvironmentActivations ALTER COLUMN WorkspaceId UNIQUEIDENTIFIER NOT NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations')
          AND c.name = N'ProjectId'
          AND c.is_nullable = 1)

        ALTER TABLE dbo.GovernanceEnvironmentActivations ALTER COLUMN ProjectId UNIQUEIDENTIFIER NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceApprovalRequests_Scope_RequestedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests'))
   AND OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceApprovalRequests_Scope_RequestedUtc
        ON dbo.GovernanceApprovalRequests (TenantId, WorkspaceId, ProjectId, RequestedUtc DESC)
        INCLUDE (
            ApprovalRequestId,
            RunId,
            Status,
            ManifestVersion,
            SourceEnvironment,
            TargetEnvironment);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernancePromotionRecords_Scope_PromotedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernancePromotionRecords'))
   AND OBJECT_ID(N'dbo.GovernancePromotionRecords', N'U') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernancePromotionRecords_Scope_PromotedUtc
        ON dbo.GovernancePromotionRecords (TenantId, WorkspaceId, ProjectId, PromotedUtc DESC)
        INCLUDE (PromotionRecordId, RunId, ManifestVersion, SourceEnvironment, TargetEnvironment);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceEnvironmentActivations_Scope_ActivatedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations'))
   AND OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceEnvironmentActivations_Scope_ActivatedUtc
        ON dbo.GovernanceEnvironmentActivations (TenantId, WorkspaceId, ProjectId, ActivatedUtc DESC)
        INCLUDE (ActivationId, RunId, Environment, IsActive, ManifestVersion);
END;
GO
