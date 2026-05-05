/*
  118: Tenant / workspace / project scope on governance workflow tables.

  Backfills from dbo.Runs (TRY_CONVERT RunId NVARCHAR to UNIQUEIDENTIFIER).
  Orphan rows (no matching Run) are removed — they cannot be scoped.
  RLS: triple predicate (TenantId, WorkspaceId, ProjectId), same as FindingFeedback.
*/

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
   AND COL_LENGTH(N'dbo.GovernanceApprovalRequests', N'TenantId') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests')
          AND c.name = N'TenantId'
          AND c.is_nullable = 0)
BEGIN
    ALTER TABLE dbo.GovernanceApprovalRequests ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.GovernanceApprovalRequests ALTER COLUMN WorkspaceId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.GovernanceApprovalRequests ALTER COLUMN ProjectId UNIQUEIDENTIFIER NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.GovernancePromotionRecords', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernancePromotionRecords', N'TenantId') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernancePromotionRecords')
          AND c.name = N'TenantId'
          AND c.is_nullable = 0)
BEGIN
    ALTER TABLE dbo.GovernancePromotionRecords ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.GovernancePromotionRecords ALTER COLUMN WorkspaceId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.GovernancePromotionRecords ALTER COLUMN ProjectId UNIQUEIDENTIFIER NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GovernanceEnvironmentActivations', N'TenantId') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations')
          AND c.name = N'TenantId'
          AND c.is_nullable = 0)
BEGIN
    ALTER TABLE dbo.GovernanceEnvironmentActivations ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.GovernanceEnvironmentActivations ALTER COLUMN WorkspaceId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.GovernanceEnvironmentActivations ALTER COLUMN ProjectId UNIQUEIDENTIFIER NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys AS fk
        WHERE fk.parent_object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests')
          AND fk.name = N'FK_GovernanceApprovalRequests_Tenants')
BEGIN
    ALTER TABLE dbo.GovernanceApprovalRequests
        ADD CONSTRAINT FK_GovernanceApprovalRequests_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id);
END;
GO

IF OBJECT_ID(N'dbo.GovernancePromotionRecords', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys AS fk
        WHERE fk.parent_object_id = OBJECT_ID(N'dbo.GovernancePromotionRecords')
          AND fk.name = N'FK_GovernancePromotionRecords_Tenants')
BEGIN
    ALTER TABLE dbo.GovernancePromotionRecords
        ADD CONSTRAINT FK_GovernancePromotionRecords_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id);
END;
GO

IF OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys AS fk
        WHERE fk.parent_object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations')
          AND fk.name = N'FK_GovernanceEnvironmentActivations_Tenants')
BEGIN
    ALTER TABLE dbo.GovernanceEnvironmentActivations
        ADD CONSTRAINT FK_GovernanceEnvironmentActivations_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GovernanceApprovalRequests_Scope_RequestedUtc'
      AND object_id = OBJECT_ID(N'dbo.GovernanceApprovalRequests'))
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
BEGIN
    CREATE NONCLUSTERED INDEX IX_GovernanceEnvironmentActivations_Scope_ActivatedUtc
        ON dbo.GovernanceEnvironmentActivations (TenantId, WorkspaceId, ProjectId, ActivatedUtc DESC)
        INCLUDE (ActivationId, RunId, Environment, IsActive, ManifestVersion);
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GovernanceApprovalRequests', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'GovernanceApprovalRequests')
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernanceApprovalRequests,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernanceApprovalRequests AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernanceApprovalRequests AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernanceApprovalRequests BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GovernancePromotionRecords', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'GovernancePromotionRecords')
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernancePromotionRecords,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernancePromotionRecords AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernancePromotionRecords AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernancePromotionRecords BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'GovernanceEnvironmentActivations')
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernanceEnvironmentActivations,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernanceEnvironmentActivations AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernanceEnvironmentActivations AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.GovernanceEnvironmentActivations BEFORE DELETE;
END;
GO
